﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Typemaker.Compiler.Settings
{
	public sealed class ByondVersionDefinition
	{
		ByondVersion Min { get; set; }
		ByondVersion Max { get; set; }
		ByondVersion Target { get; set; }
	}
}
