﻿using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Ngram
{

    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */
    /// <summary>
    /// Creates new instances of <seealso cref="EdgeNGramTokenFilter"/>.
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_edgngrm" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.EdgeNGramFilterFactory" minGramSize="1" maxGramSize="1"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class EdgeNGramFilterFactory : TokenFilterFactory
    {
        private readonly int maxGramSize;
        private readonly int minGramSize;
        private readonly string side;

        /// <summary>
        /// Creates a new EdgeNGramFilterFactory </summary>
        public EdgeNGramFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            minGramSize = getInt(args, "minGramSize", EdgeNGramTokenFilter.DEFAULT_MIN_GRAM_SIZE);
            maxGramSize = getInt(args, "maxGramSize", EdgeNGramTokenFilter.DEFAULT_MAX_GRAM_SIZE);
            side = get(args, "side", EdgeNGramTokenFilter.Side.FRONT.Label);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new EdgeNGramTokenFilter(luceneMatchVersion, input, side, minGramSize, maxGramSize);
        }
    }
}