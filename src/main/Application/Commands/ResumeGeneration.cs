﻿using CQRSlite.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Application.Commands
{
    public class ResumeGeneration : ICommand
    {
        public int ExpectedVersion => throw new NotImplementedException();

        public ResumeGeneration()
        {
        }
    }
}
