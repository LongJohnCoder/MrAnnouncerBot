﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace DHDM
{
	public class SavingThrowCommand : BaseStreamDeckCommand, IDungeonMasterCommand
	{
		string savingThrow;
		public void Execute(IDungeonMasterApp dungeonMasterApp, ChatMessage chatMessage)
		{
			dungeonMasterApp.PrepareSavingThrow(savingThrow);
		}

		public bool Matches(string message)
		{
			Match match = Regex.Match(message, $"^Save\\s+(\\w+)");
			if (match.Success)
			{
				savingThrow = match.Groups[1].Value;
				return true;
			}
			return false;
		}
	}
}
