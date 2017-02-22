// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace DbStressTest
{
    [Serializable]
    internal sealed class StopExecutionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StopExecutionException()
        {
        }

        public StopExecutionException(string message)
            : base(message)
        {
        }

        public StopExecutionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        internal StopExecutionException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
