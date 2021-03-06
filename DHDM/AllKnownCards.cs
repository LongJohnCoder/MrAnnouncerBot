﻿//#define profiling
using System;
using System.Linq;
using System.Collections.Generic;
using Streamloots;
using GoogleHelper;

namespace DHDM
{
	public static class AllKnownCards
	{
		private static List<T> LoadData<T>() where T : new()
		{
			return GoogleSheets.Get<T>();
		}

		public static CardEventData Get(string cardId)
		{
			return Cards.FirstOrDefault(x => x.ID == cardId);
		}

		static void Load()
		{
			allKnownCards = LoadData<CardEventData>();
		}
		static List<CardEventData> allKnownCards;

		public static void Invalidate()
		{
			allKnownCards = null;
		}

		public static CardEventData Get(CardDto cardDto)
		{
			string cardId = cardDto.Card.soundUrl; // Yeah, I know. But trust me. This is the only way to round trip our CardDto.
			return Get(cardId);
		}

		public static List<CardEventData> Cards
		{
			get
			{
				if (allKnownCards == null)
					Load();
				return allKnownCards;
			}
		}

	}
}
