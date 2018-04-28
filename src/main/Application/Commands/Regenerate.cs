using CQRSlite.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Application.Commands
{
    public class Regenerate : ICommand
    {
        public int ExpectedVersion => throw new NotImplementedException();

        public Regenerate(string avatarId)
        {
            this.AvatarId = avatarId;
        }

        public string AvatarId { get; set; }
    }
}
