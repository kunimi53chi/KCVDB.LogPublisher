﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ProtoBuf;

namespace KCVDB.LogFilePublisher
{
    sealed class Publisher : IDisposable
    {
        public Publisher(string statePath, string outputPath)
        {
            PublisherState state;
            this.stateFileInfo = new FileInfo(statePath);
            if (this.stateFileInfo.Exists)
            {
                using (var stream = this.stateFileInfo.OpenRead())
                {
                    state = Serializer.Deserialize<PublisherState>(stream);
                }
                this.sessionInfos = state.SessionInfos;
                this.memberIds = state.MemberIds;
                this.publicMemberIds = new HashSet<int>(this.memberIds.Values);
            }
            else
            {
                this.sessionInfos = new Dictionary<Guid, SessionInfo>();
                this.memberIds = new Dictionary<int, int>();
                this.publicMemberIds = new HashSet<int>();
            }
            this.outputDirectoryInfo = new DirectoryInfo(outputPath);
            this.outputDirectoryInfo.Create();
        }

        public bool Add(string line)
        {
            if (line != null)
            {
                var row = new Row(line);
                var uriMatch = uriRegex.Match(row.RequestUri);
                if (uriMatch.Groups["payment"].Success)
                {
                    this.publicSessionId = null;
                }
                else
                {
                    var currentPrivateSessionId = new Guid(row.SessionId);
                    if (this.privateSessionId == null || this.privateSessionId != currentPrivateSessionId)
                    {
                        this.privateSessionId = currentPrivateSessionId;
                        this.publicSessionId = null;
                    }
                    var lineHash = Hash.Compute(line);
                    if (this.publicSessionId == null)
                    {
                        SessionInfo sessionInfo;
                        if (this.sessionInfos.TryGetValue(this.privateSessionId.Value, out sessionInfo) && sessionInfo.LineHash == lineHash)
                        {
                            this.publicSessionId = sessionInfo.PublicSessionId;
                        }
                        else
                        {
                            this.publicSessionId = Guid.NewGuid();
                        }
                        this.sessionInfos[this.privateSessionId.Value] = new SessionInfo { PublicSessionId = this.publicSessionId.Value };
                        this.writer?.Dispose();
                        this.writer = new StreamWriter(File.OpenWrite(Path.Combine(this.outputDirectoryInfo.FullName, $"{this.publicSessionId}.log")));
                        Console.WriteLine($"{this.privateSessionId} => {this.publicSessionId}");
                    }
                    this.sessionInfos[this.privateSessionId.Value].LineHash = lineHash;
                    row.RequestValue = requestRegex.Replace(row.RequestValue, match =>
                    {
                        if (match.Groups["id"].Success)
                        {
                            return $"{match.Groups["key"]}0";
                        }
                        else
                        {
                            return $"{match.Groups["key"]}";
                        }
                    });
                    if (uriMatch.Groups["deck"].Success)
                    {
                        row.ResponseValue = deckResponseRegex.Replace(row.ResponseValue, EvaluateResponse);
                    }
                    else
                    {
                        row.ResponseValue = responseRegex.Replace(row.ResponseValue, EvaluateResponse);
                    }
                    this.writer.WriteLine(row.ToString());
                }
                return true;
            }
            else
            {
                this.privateSessionId = null;
                return false;
            }
        }

        public string EvaluateResponse(Match match)
        {
            if (match.Groups["number"].Success)
            {
                int value;
                if (match.Groups["key"].Value == "api_member_id")
                {
                    value = ToPublicMemberId(int.Parse(match.Groups["number"].Value));
                }
                else
                {
                    value = 0;
                }
                return $"{match.Groups["prefix"].Value}{value}";
            }
            else if (match.Groups["string"].Success)
            {
                string value;
                if (match.Groups["key"].Value == "api_member_id")
                {
                    value = ToPublicMemberId(int.Parse(match.Groups["string"].Value)).ToString();
                }
                else if (match.Groups["id"].Success)
                {
                    value = "0";
                }
                else
                {
                    value = "";
                }
                return $"{match.Groups["prefix"].Value}\"{value}\"";
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public int ToPublicMemberId(int privateMemberId)
        {
            int publicMemberId;
            if (!this.memberIds.TryGetValue(privateMemberId, out publicMemberId))
            {
                do
                {
                    var data = new byte[4];
                    randomNumberGenerator.GetBytes(data);
                    publicMemberId = checked((int)(1U << 30 | (((1U << 30) - 1U) & ((uint)data[3] << 24 | (uint)data[2] << 16 | (uint)data[1] << 8 | (uint)data[0]))));
                } while (this.publicMemberIds.Contains(publicMemberId));
                this.publicMemberIds.Add(publicMemberId);
                this.memberIds.Add(privateMemberId, publicMemberId);
            }
            return publicMemberId;
        }

        public void Dispose()
        {
            this.writer?.Dispose();
            File.Move(this.stateFileInfo.FullName, $"{this.stateFileInfo.FullName}.bak");
            using (var stream = this.stateFileInfo.OpenWrite())
            {
                Serializer.Serialize(stream, new PublisherState { SessionInfos = this.sessionInfos, MemberIds = this.memberIds });
            }
        }

        static Publisher()
        {
            randomNumberGenerator = RandomNumberGenerator.Create();
            uriRegex = new Regex("/kcsapi/api_(?:(?<payment>dmm_payment/.*|get_member/payitem|req_member/payitemuse/)|(?<deck>get_member/(?:preset_)?deck))$", RegexOptions.Compiled);
            requestRegex = new Regex("(?:^|(?<=&))(?<key>api(?:%5[Ff]|_)(?:name|cmt)(?<id>_id)?=)(?<value>[^&]*)", RegexOptions.Compiled);
            var ws = "[\\x09\\x0A\\x0D\\x20]*";
            var value = "(?:(?<number>[^\\x09\\x0A\\x0D\\x20\",:\\[\\]\\{\\}]+)|\"(?<string>(?:\\\\u....|\\\\[^u]|[^\\\\\"])*)\")";
            var lookbehind = "(?:^|(?<=[^\\\\]))";
            deckResponseRegex = new Regex($"{lookbehind}(?<prefix>\"(?<key>api_(?:enemy_)?(?:comment|deck_?name|member|name|nickname)(?<id>_id)?)\"{ws}:{ws}){value}", RegexOptions.Compiled);
            responseRegex = new Regex($"{lookbehind}(?<prefix>\"(?<key>api_(?:enemy_)?(?:comment|deck_?name|member|name|nickname)(?<id>_id)?)\"{ws}:{ws}){value}", RegexOptions.Compiled);
        }

        private static readonly RandomNumberGenerator randomNumberGenerator;
        private static readonly Regex uriRegex;
        private static readonly Regex requestRegex;
        private static readonly Regex deckResponseRegex;
        private static readonly Regex responseRegex;
        private readonly FileInfo stateFileInfo;
        private readonly Dictionary<Guid, SessionInfo> sessionInfos;
        private readonly Dictionary<int, int> memberIds;
        private readonly HashSet<int> publicMemberIds;
        private readonly DirectoryInfo outputDirectoryInfo;
        private StreamWriter writer;
        private Guid? privateSessionId;
        private Guid? publicSessionId;
    }
}
