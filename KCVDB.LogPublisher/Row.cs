using System;

namespace KCVDB.LogFilePublisher
{
    internal sealed class Row
    {
        public string AgentId;
        public string SessionId;
        public string RequestUri;
        public string StatusCodeString;
        public string HttpDateString;
        public string LocalTimeString;
        public string RequestValue;
        public string ResponseValue;

        public Row(string line)
        {
            var cells = line.Split(new[] { "\t" }, StringSplitOptions.None);
            if (cells.Length == 8)
            {
                this.AgentId = cells[0];
                this.SessionId = cells[1];
                this.RequestUri = cells[2];
                this.StatusCodeString = cells[3];
                this.HttpDateString = cells[4];
                this.LocalTimeString = cells[5];
                this.RequestValue = cells[6];
                this.ResponseValue = cells[7];
            }
            else
            {
                throw new FormatException();
            }
        }

        public override string ToString()
        {
            return string.Join("\t", new string[]
                {
                    this.AgentId,
                    this.SessionId,
                    this.RequestUri,
                    this.StatusCodeString,
                    this.HttpDateString,
                    this.LocalTimeString,
                    this.RequestValue,
                    this.ResponseValue
                });
        }
    }
}
