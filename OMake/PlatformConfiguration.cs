using System;
using System.Collections.Generic;

namespace OMake
{
	/// <summary>
	/// The configuration for a single platform.
	/// </summary>
	public class PlatformConfiguration
	{
		public Dictionary<string, string> Constants = new Dictionary<string, string>();
		public Dictionary<string, string> Tools = new Dictionary<string, string>();
	}
}
