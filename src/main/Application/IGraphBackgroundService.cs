using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Application
{
	public interface IGraphBackgroundService
	{
		void Regenerate();

		void ResumeGeneration();

		void Suspend();
	}
}