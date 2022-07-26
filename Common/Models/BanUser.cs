using System;

namespace Common.Models
{
    public class BanUser
    {
        public DateTime LastMessageTime { get; set; }
        public int PenaltyCount { get; set; }
        public bool PenaltyMessageSended { get; set; }
        public bool IsBanned => PenaltyCount >= 2;
    }
}
