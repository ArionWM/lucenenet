﻿using System;
using System.Diagnostics;

namespace org.apache.lucene.store
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with this
	 * work for additional information regarding copyright ownership. The ASF
	 * licenses this file to You under the Apache License, Version 2.0 (the
	 * "License"); you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 * 
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	 * License for the specific language governing permissions and limitations under
	 * the License.
	 */

	ignore
	import java.nio.channels.FileChannel;

	import org.apache.lucene.store.Directory; // javadoc
	import org.apache.lucene.store.IOContext.Context;

	// TODO
	//   - newer Linux kernel versions (after 2.6.29) have
	//     improved MADV_SEQUENTIAL (and hopefully also
	//     FADV_SEQUENTIAL) interaction with the buffer
	//     cache; we should explore using that instead of direct
	//     IO when context is merge

	/// <summary>
	/// A <seealso cref="Directory"/> implementation for all Unixes that uses
	/// DIRECT I/O to bypass OS level IO caching during
	/// merging.  For all other cases (searching, writing) we delegate
	/// to the provided Directory instance.
	/// 
	/// <para>See <a
	/// href="{@docRoot}/overview-summary.html#NativeUnixDirectory">Overview</a>
	/// for more details.
	/// 
	/// </para>
	/// <para>To use this you must compile
	/// NativePosixUtil.cpp (exposes Linux-specific APIs through
	/// JNI) for your platform, by running <code>ant
	/// build-native-unix</code>, and then putting the resulting
	/// <code>libNativePosixUtil.so</code> (from
	/// <code>lucene/build/native</code>) onto your dynamic
	/// linker search path.
	/// 
	/// </para>
	/// <para><b>WARNING</b>: this code is very new and quite easily
	/// could contain horrible bugs.  For example, here's one
	/// known issue: if you use seek in <code>IndexOutput</code>, and then
	/// write more than one buffer's worth of bytes, then the
	/// file will be wrong.  Lucene does not do this today (only writes
	/// small number of bytes after seek), but that may change.
	/// 
	/// </para>
	/// <para>This directory passes Solr and Lucene tests on Linux
	/// and OS X; other Unixes should work but have not been
	/// tested!  Use at your own risk.
	/// 
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public class NativeUnixDirectory extends FSDirectory
	{

	  // TODO: this is OS dependent, but likely 512 is the LCD
	  private final static long ALIGN = 512;
	  private final static long ALIGN_NOT_MASK = ~(ALIGN - 1);

	  /// <summary>
	  /// Default buffer size before writing to disk (256 KB);
	  ///  larger means less IO load but more RAM and direct
	  ///  buffer storage space consumed during merging. 
	  /// </summary>

	  public final static int DEFAULT_MERGE_BUFFER_SIZE = 262144;

	  /// <summary>
	  /// Default min expected merge size before direct IO is
	  ///  used (10 MB): 
	  /// </summary>
	  public final static long DEFAULT_MIN_BYTES_DIRECT = 10 * 1024 * 1024;

	  private final int mergeBufferSize;
	  private final long minBytesDirect;
	  private final Directory @delegate;

	  /// <summary>
	  /// Create a new NIOFSDirectory for the named location.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <param name="mergeBufferSize"> Size of buffer to use for
	  ///    merging.  See <seealso cref="#DEFAULT_MERGE_BUFFER_SIZE"/>. </param>
	  /// <param name="minBytesDirect"> Merges, or files to be opened for
	  ///   reading, smaller than this will
	  ///   not use direct IO.  See {@link
	  ///   #DEFAULT_MIN_BYTES_DIRECT} </param>
	  /// <param name="delegate"> fallback Directory for non-merges </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
	  public NativeUnixDirectory(File path, int mergeBufferSize, long minBytesDirect, Directory @delegate) throws IOException
	  {
		base(path, @delegate.LockFactory);
		if ((mergeBufferSize & ALIGN) != 0)
		{
		  throw new System.ArgumentException("mergeBufferSize must be 0 mod " + ALIGN + " (got: " + mergeBufferSize + ")");
		}
		this.mergeBufferSize = mergeBufferSize;
		this.minBytesDirect = minBytesDirect;
		this.@delegate = @delegate;
	  }

	  /// <summary>
	  /// Create a new NIOFSDirectory for the named location.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <param name="delegate"> fallback Directory for non-merges </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
	  public NativeUnixDirectory(File path, Directory @delegate) throws IOException
	  {
		this(path, DEFAULT_MERGE_BUFFER_SIZE, DEFAULT_MIN_BYTES_DIRECT, @delegate);
	  }

	  public IndexInput openInput(string name, IOContext context) throws IOException
	  {
		ensureOpen();
		if (context.context != Context.MERGE || context.mergeInfo.estimatedMergeBytes < minBytesDirect || fileLength(name) < minBytesDirect)
		{
		  return @delegate.openInput(name, context);
		}
		else
		{
		  return new NativeUnixIndexInput(new File(Directory, name), mergeBufferSize);
		}
	  }

	  public IndexOutput createOutput(string name, IOContext context) throws IOException
	  {
		ensureOpen();
		if (context.context != Context.MERGE || context.mergeInfo.estimatedMergeBytes < minBytesDirect)
		{
		  return @delegate.createOutput(name, context);
		}
		else
		{
		  ensureCanWrite(name);
		  return new NativeUnixIndexOutput(new File(Directory, name), mergeBufferSize);
		}
	  }

	  private final static class NativeUnixIndexOutput extends IndexOutput
	  {
		private final ByteBuffer buffer;
		private final FileOutputStream fos;
		private final FileChannel channel;
		private final int bufferSize;

		//private final File path;

		private int bufferPos;
		private long filePos;
		private long fileLength;
		private bool isOpen;

		public NativeUnixIndexOutput(File path, int bufferSize) throws IOException
		{
		  //this.path = path;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.FileDescriptor fd = NativePosixUtil.open_direct(path.toString(), false);
		  FileDescriptor fd = NativePosixUtil.open_direct(path.ToString(), false);
		  fos = new FileOutputStream(fd);
		  //fos = new FileOutputStream(path);
		  channel = fos.Channel;
		  buffer = ByteBuffer.allocateDirect(bufferSize);
		  this.bufferSize = bufferSize;
		  isOpen = true;
		}

		public void writeByte(sbyte b) throws IOException
		{
		  Debug.Assert(bufferPos == buffer.position(), "bufferPos=" + bufferPos + " vs buffer.position()=" + buffer.position());
		  buffer.put(b);
		  if (++bufferPos == bufferSize)
		  {
			dump();
		  }
		}

		public void writeBytes(sbyte[] src, int offset, int len) throws IOException
		{
		  int toWrite = len;
		  while (true)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int left = bufferSize - bufferPos;
			int left = bufferSize - bufferPos;
			if (left <= toWrite)
			{
			  buffer.put(src, offset, left);
			  toWrite -= left;
			  offset += left;
			  bufferPos = bufferSize;
			  dump();
			}
			else
			{
			  buffer.put(src, offset, toWrite);
			  bufferPos += toWrite;
			  break;
			}
		  }
		}

		//@Override
		//public void setLength() throws IOException {
		//   TODO -- how to impl this?  neither FOS nor
		//   FileChannel provides an API?
		//}

		public void flush()
		{
		  // TODO -- I don't think this method is necessary?
		}

		private void dump() throws IOException
		{
		  buffer.flip();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long limit = filePos + buffer.limit();
		  long limit = filePos + buffer.limit();
		  if (limit > fileLength)
		  {
			// this dump extends the file
			fileLength = limit;
		  }
		  else
		  {
			// we had seek'd back & wrote some changes
		  }

		  // must always round to next block
		  buffer.limit((int)((buffer.limit() + ALIGN - 1) & ALIGN_NOT_MASK));

		  assert(buffer.limit() & ALIGN_NOT_MASK) == buffer.limit() : "limit=" + buffer.limit() + " vs " + (buffer.limit() & ALIGN_NOT_MASK);
		  assert(filePos & ALIGN_NOT_MASK) == filePos;
		  //System.out.println(Thread.currentThread().getName() + ": dump to " + filePos + " limit=" + buffer.limit() + " fos=" + fos);
		  channel.write(buffer, filePos);
		  filePos += bufferPos;
		  bufferPos = 0;
		  buffer.clear();
		  //System.out.println("dump: done");

		  // TODO: the case where we'd seek'd back, wrote an
		  // entire buffer, we must here read the next buffer;
		  // likely Lucene won't trip on this since we only
		  // write smallish amounts on seeking back
		}

		public long FilePointer
		{
		  return filePos + bufferPos;
		}

		// TODO: seek is fragile at best; it can only properly
		// handle seek & then change bytes that fit entirely
		// within one buffer
		public void seek(long pos) throws IOException
		{
		  if (pos != FilePointer)
		  {
			dump();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long alignedPos = pos & ALIGN_NOT_MASK;
			long alignedPos = pos & ALIGN_NOT_MASK;
			filePos = alignedPos;
			int n = (int) NativePosixUtil.pread(fos.FD, filePos, buffer);
			if (n < bufferSize)
			{
			  buffer.limit(n);
			}
			//System.out.println("seek refill=" + n);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delta = (int)(pos - alignedPos);
			int delta = (int)(pos - alignedPos);
			buffer.position(delta);
			bufferPos = delta;
		  }
		}

		public long length()
		{
		  return fileLength + bufferPos;
		}

		public long Checksum throws IOException
		{
		  throw new System.NotSupportedException("this directory currently does not work at all!");
		}

		public void close() throws IOException
		{
		  if (isOpen)
		  {
			isOpen = false;
			try
			{
			  dump();
			}
			finally
			{
			  try
			  {
				//System.out.println("direct close set len=" + fileLength + " vs " + channel.size() + " path=" + path);
				channel.truncate(fileLength);
				//System.out.println("  now: " + channel.size());
			  }
			  finally
			  {
				try
				{
				  channel.close();
				}
				finally
				{
				  fos.close();
				  //System.out.println("  final len=" + path.length());
				}
			  }
			}
		  }
		}
	  }

	  private final static class NativeUnixIndexInput extends IndexInput
	  {
		private final ByteBuffer buffer;
		private final FileInputStream fis;
		private final FileChannel channel;
		private final int bufferSize;

		private bool isOpen;
		private bool isClone;
		private long filePos;
		private int bufferPos;

		public NativeUnixIndexInput(File path, int bufferSize) throws IOException
		{
		  base("NativeUnixIndexInput(path=\"" + path.Path + "\")");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.FileDescriptor fd = NativePosixUtil.open_direct(path.toString(), true);
		  FileDescriptor fd = NativePosixUtil.open_direct(path.ToString(), true);
		  fis = new FileInputStream(fd);
		  channel = fis.Channel;
		  this.bufferSize = bufferSize;
		  buffer = ByteBuffer.allocateDirect(bufferSize);
		  isOpen = true;
		  isClone = false;
		  filePos = -bufferSize;
		  bufferPos = bufferSize;
		  //System.out.println("D open " + path + " this=" + this);
		}

		// for clone
		public NativeUnixIndexInput(NativeUnixIndexInput other) throws IOException
		{
		  base(other.ToString());
		  this.fis = null;
		  channel = other.channel;
		  this.bufferSize = other.bufferSize;
		  buffer = ByteBuffer.allocateDirect(bufferSize);
		  filePos = -bufferSize;
		  bufferPos = bufferSize;
		  isOpen = true;
		  isClone = true;
		  //System.out.println("D clone this=" + this);
		  seek(other.FilePointer);
		}

		public void close() throws IOException
		{
		  if (isOpen && !isClone)
		  {
			try
			{
			  channel.close();
			}
			finally
			{
			  if (!isClone)
			  {
				fis.close();
			  }
			}
		  }
		}

		public long FilePointer
		{
		  return filePos + bufferPos;
		}

		public void seek(long pos) throws IOException
		{
		  if (pos != FilePointer)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long alignedPos = pos & ALIGN_NOT_MASK;
			long alignedPos = pos & ALIGN_NOT_MASK;
			filePos = alignedPos - bufferSize;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delta = (int)(pos - alignedPos);
			int delta = (int)(pos - alignedPos);
			if (delta != 0)
			{
			  refill();
			  buffer.position(delta);
			  bufferPos = delta;
			}
			else
			{
			  // force refill on next read
			  bufferPos = bufferSize;
			}
		  }
		}

		public long length()
		{
		  try
		  {
			return channel.size();
		  }
		  catch (IOException ioe)
		  {
			throw new Exception("IOException during length(): " + this, ioe);
		  }
		}

		public sbyte readByte() throws IOException
		{
		  // NOTE: we don't guard against EOF here... ie the
		  // "final" buffer will typically be filled to less
		  // than bufferSize
		  if (bufferPos == bufferSize)
		  {
			refill();
		  }
		  Debug.Assert(bufferPos == buffer.position(), "bufferPos=" + bufferPos + " vs buffer.position()=" + buffer.position());
		  bufferPos++;
		  return buffer.get();
		}

		private void refill() throws IOException
		{
		  buffer.clear();
		  filePos += bufferSize;
		  bufferPos = 0;
		  assert(filePos & ALIGN_NOT_MASK) == filePos : "filePos=" + filePos + " anded=" + (filePos & ALIGN_NOT_MASK);
		  //System.out.println("X refill filePos=" + filePos);
		  int n;
		  try
		  {
			n = channel.read(buffer, filePos);
		  }
		  catch (IOException ioe)
		  {
			throw new IOException(ioe.Message + ": " + this, ioe);
		  }
		  if (n < 0)
		  {
			throw new EOFException("read past EOF: " + this);
		  }
		  buffer.rewind();
		}

		public void readBytes(sbyte[] dst, int offset, int len) throws IOException
		{
		  int toRead = len;
		  //System.out.println("\nX readBytes len=" + len + " fp=" + getFilePointer() + " size=" + length() + " this=" + this);
		  while (true)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int left = bufferSize - bufferPos;
			int left = bufferSize - bufferPos;
			if (left < toRead)
			{
			  //System.out.println("  copy " + left);
			  buffer.get(dst, offset, left);
			  toRead -= left;
			  offset += left;
			  refill();
			}
			else
			{
			  //System.out.println("  copy " + toRead);
			  buffer.get(dst, offset, toRead);
			  bufferPos += toRead;
			  //System.out.println("  readBytes done");
			  break;
			}
		  }
		}

		public NativeUnixIndexInput MemberwiseClone()
		{
		  try
		  {
			return new NativeUnixIndexInput(this);
		  }
		  catch (IOException ioe)
		  {
			throw new Exception("IOException during clone: " + this, ioe);
		  }
		}
	  }
	}

}