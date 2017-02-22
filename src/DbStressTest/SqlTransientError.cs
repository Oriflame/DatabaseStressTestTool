namespace DbStressTest
{
    internal enum SqlTransientError 
    {
        Unknown = 0,
        /// <summary>
        /// The request limit for the database is XX and has been reached. See 'http://go.microsoft.com/fwlink/?LinkId=267637' for assistance.
        /// </summary>
        RequestLimit = 10928
    }
}
