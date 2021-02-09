﻿using System;
using System.Linq;

namespace DndCore
{
	public class AskValueEventArgs : EventArgs
	{

		public AskValueEventArgs()
		{

		}

		
		public AskValueEventArgs(Creature player, string caption, string memberName, string memberTypeName, object value)
		{
			MemberName = memberName;
			Value = value;
			MemberTypeName = memberTypeName;
			Caption = caption;
			Player = player;
		}

		public Creature Player { get; set; }
		public string Caption { get; set; }
		public object Value { get; set; }
		public string MemberTypeName { get; set; }
		public string MemberName { get; set; }
	}
}

