using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KCVDB.LogFilePublisher
{
    sealed class PublisherState : IDisposable
    {
        public readonly Dictionary<Guid, SessionInfo> SessionInfos;

        public readonly Dictionary<int, int> MemberInfos;

        private readonly string outputPath;

        public PublisherState(string inputPath, string outputPath)
        {
            var sessionsFileInfo = inputPath != null ? new FileInfo(Path.Combine(inputPath, "Sessions.tsv")) : null;
            if (sessionsFileInfo?.Exists ?? false)
            {
                using (var reader = new StreamReader(sessionsFileInfo.OpenRead(), Encoding.UTF8))
                {
                    this.SessionInfos = ReadAllRows(reader, 3)
                        .ToDictionary(row => new Guid(row[0]), row => new SessionInfo { PublicSessionId = new Guid(row[1]), LineHash = new Hash(row[2]) });
                }
            }
            else
            {
                this.SessionInfos = new Dictionary<Guid, SessionInfo>();
            }
            var membersFileInfo = inputPath != null ? new FileInfo(Path.Combine(inputPath, "Members.tsv")) : null;
            if (membersFileInfo?.Exists ?? false)
            {
                using (var reader = new StreamReader(membersFileInfo.OpenRead(), Encoding.UTF8))
                {
                    this.MemberInfos = ReadAllRows(reader, 2)
                        .ToDictionary(row => int.Parse(row[0]), row => int.Parse(row[1]));
                }
            }
            else
            {
                this.MemberInfos = new Dictionary<int, int>();
            }
            this.outputPath = outputPath;
        }

        private static IEnumerable<string[]> ReadAllRows(TextReader reader, int columnCount)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var row = line.Split(new[] { "\t" }, StringSplitOptions.None);
                if (row.Length != columnCount)
                {
                    throw new ArgumentException();
                }
                yield return row;
            }
        }

        public void Dispose()
        {
            var membersFileInfo = new FileInfo(Path.Combine(this.outputPath, "Members.tsv"));
            membersFileInfo.Directory.Create();
            using (var writer = new StreamWriter(membersFileInfo.OpenWrite(), Encoding.UTF8))
            {
                foreach (var memberInfo in this.MemberInfos.OrderBy(item => item.Key))
                {
                    writer.WriteLine($"{memberInfo.Key}\t{memberInfo.Value}");
                }
            }
            var sessionsFileInfo = new FileInfo(Path.Combine(this.outputPath, "Sessions.tsv"));
            sessionsFileInfo.Directory.Create();
            using (var writer = new StreamWriter(sessionsFileInfo.OpenWrite(), Encoding.UTF8))
            {
                foreach (var sessionInfo in this.SessionInfos.OrderBy(item => item.Key))
                {
                    writer.WriteLine($"{sessionInfo.Key}\t{sessionInfo.Value.PublicSessionId}\t{sessionInfo.Value.LineHash}");
                }
            }
        }
    }


    sealed class SessionInfo
    {
        public Guid PublicSessionId;

        public Hash LineHash;
    }
}
