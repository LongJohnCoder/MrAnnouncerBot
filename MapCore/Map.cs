﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MapCore;

namespace MapCore
{
	public class Map : IMapInterface
	{
		private const string MapFolder = @"D:\Dropbox\DX\Twitch\CodeRushed\MrAnnouncerBot\OverlayManager\wwwroot\GameDev\Assets\DragonH\Maps";

		int lastRowIndex;
		int lastColumnIndex;
		int rightmostColumnIndex;
		int wallChangeCount;
		public Map()
		{

		}
		void Reset()
		{
			Tiles = new List<Tile>();
			lastRowIndex = -1;
			lastColumnIndex = -1;
			rightmostColumnIndex = -1;
		}
		void OnNewLine()
		{
			lastRowIndex++;
			lastColumnIndex = -1;
		}
		bool IsFloor(string space)
		{
			// TODO: Check for code for Portcullis.
			return space == "F" || space.StartsWith("D") || space.StartsWith("S");
		}
		void LoadNewSpace(string space)
		{
			OnNewSpace();
			if (IsFloor(space))
				Tiles.Add(new FloorSpace(lastColumnIndex, lastRowIndex, space, this));
			else
				Tiles.Add(new EmptySpace(lastColumnIndex, lastRowIndex, this));
		}
		void OnNewSpace()
		{
			lastColumnIndex++;
			if (lastColumnIndex > rightmostColumnIndex)
				rightmostColumnIndex = lastColumnIndex;
		}

		public void LoadData(string mapData)
		{
			ProcessLines(SplitLines(mapData));
		}

		public void Load(string fileName)
		{
			LoadData(GetDataFromFile(fileName));
		}

		private void ProcessLines(string[] lines)
		{
			Reset();
			foreach (var line in lines)
				LoadNewLine(line);

			SetMapSize();
			BuildMapArrays();
			GetAllRoomsAndCorridors();
		}

		public void BuildMapArrays()
		{
			AllTiles = new Tile[NumColumns, NumRows];
			_allVerticalWalls = new bool[NumColumns + 1, NumRows + 1];
			_allHorizontalWalls = new bool[NumColumns + 1, NumRows + 1];
			// TODO: Find walls for rooms that are loaded.

			foreach (Tile space in Tiles)
			{
				AllTiles[space.Column, space.Row] = space;
			}
		}

		private void SetMapSize()
		{
			NumColumns = rightmostColumnIndex + 1;
			NumRows = lastRowIndex + 1;
		}

		private static string GetDataFromFile(string fileName)
		{
			return File.ReadAllText(Path.Combine(MapFolder, fileName));
		}

		private static string[] SplitLines(string text)
		{
			return TrimEmptyLines(text.Split('\n'));
		}
		static string[] TrimEmptyLines(string[] lines)
		{
			int endLinesToSkip = 0;
			for (int i = lines.Length - 1; i >= 0; i--)
			{
				if (string.IsNullOrWhiteSpace(lines[i]))
					endLinesToSkip++;
				else
					break;
			}
			int startLinesToSkip = lines.TakeWhile(x => string.IsNullOrWhiteSpace(x)).ToList().Count;

			int endIndex = lines.Length - endLinesToSkip - startLinesToSkip- 1;
			string[] result = lines.SkipWhile(x => string.IsNullOrWhiteSpace(x)).TakeWhile((line, index) => index <= endIndex).ToArray();
			return result;
		}


		void LoadNewLine(string line)
		{
			line = line.Trim('\r');
			OnNewLine();
			var spaces5x5 = line.Split('\t');
			foreach (var space in spaces5x5)
				LoadNewSpace(space);
		}

		static void ClearAllSpaces(Tile[,] mapArray, int numColumns, int numRows)
		{
			for (int column = 0; column < numColumns; column++)
				for (int row = 0; row < numRows; row++)
				{
					FloorSpace activeSpace = mapArray[column, row] as FloorSpace;
					if (activeSpace != null)
					{
						activeSpace.Parent = null;
						activeSpace.SpaceType = SpaceType.None;
					}
				}
		}

		public FloorSpace GetFloorSpace(int column, int row)
		{
			if (column < 0 || row < 0 || column >= NumColumns || row >= NumRows)
				return null;
			return AllTiles[column, row] as FloorSpace;
		}

		public Tile GetTile(int column, int row)
		{
			if (column < 0 || row < 0 || column >= NumColumns || row >= NumRows)
				return null;
			return AllTiles[column, row];
		}

		public bool FloorSpaceExists(int column, int row)
		{
			return GetFloorSpace(column, row) != null;
		}

		bool HasThreeAdjacentSpaces(int column, int row)
		{
			FloorSpace space = GetFloorSpace(column, row);
			if (space == null)
				return false;

			//`![](8F1819625E627683BB9C334475F3CEAB.png)
			if (FloorSpaceExists(column - 1, row - 1) &&
					FloorSpaceExists(column, row - 1) &&
					FloorSpaceExists(column - 1, row))
				return true;

			//`![](EA83D3AD6427364DB9E7C9A58DF3AB67.png)
			if (FloorSpaceExists(column + 1, row - 1) &&
					FloorSpaceExists(column, row - 1) &&
					FloorSpaceExists(column + 1, row))
				return true;

			//`![](2BFF98EE7BEAF2BFB29187399968E481.png)
			if (FloorSpaceExists(column + 1, row + 1) &&
					FloorSpaceExists(column, row + 1) &&
					FloorSpaceExists(column + 1, row))
				return true;

			//`![](EC9B46DCDCC425B9664795DEB9997596.png)
			if (FloorSpaceExists(column - 1, row + 1) &&
					FloorSpaceExists(column, row + 1) &&
					FloorSpaceExists(column - 1, row))
				return true;

			return false;
		}

		void AddIfAvailable(FloorSpace floorSpace, MapRegion region, FloorSpace activeSpace)
		{
			if (floorSpace == null)
				return;
			if (floorSpace.SpaceType == activeSpace.SpaceType && floorSpace.Parent == null)
			{
				floorSpace.Parent = region;
				AddAdjacentSpaces(region, floorSpace);
			}
		}

		void AddAdjacentSpaces(MapRegion region, FloorSpace activeSpace)
		{
			region.Spaces.Add(activeSpace);

			FloorSpace left = GetFloorSpace(activeSpace.Column - 1, activeSpace.Row);
			AddIfAvailable(left, region, activeSpace);

			FloorSpace top = GetFloorSpace(activeSpace.Column, activeSpace.Row - 1);
			AddIfAvailable(top, region, activeSpace);

			FloorSpace right = GetFloorSpace(activeSpace.Column + 1, activeSpace.Row);
			AddIfAvailable(right, region, activeSpace);

			FloorSpace bottom = GetFloorSpace(activeSpace.Column, activeSpace.Row + 1);
			AddIfAvailable(bottom, region, activeSpace);
		}

		void AddSpaceToRegion(MapRegion region, FloorSpace activeSpace)
		{
			activeSpace.Parent = region;
			AddAdjacentSpaces(region, activeSpace);
		}

		Room CreateRoomFromSpace(FloorSpace activeSpace)
		{
			Room room = new Room();
			AddSpaceToRegion(room, activeSpace);
			return room;
		}

		Corridor CreateCorridorFromSpace(FloorSpace activeSpace)
		{
			Corridor corridor = new Corridor();
			AddSpaceToRegion(corridor, activeSpace);
			return corridor;
		}

		public void GetAllRoomsAndCorridors()
		{
			Rooms.Clear();
			Corridors.Clear();
			ClearAllSpaces(AllTiles, NumColumns, NumRows);
			DetermineSpaceTypes();


			for (int column = 0; column < NumColumns; column++)
				for (int row = 0; row < NumRows; row++)
				{
					FloorSpace activeSpace = GetFloorSpace(column, row);
					if (activeSpace == null)
						continue;

					if (activeSpace.SpaceType == SpaceType.Room)
					{
						if (activeSpace.Parent == null)
							Rooms.Add(CreateRoomFromSpace(activeSpace));
					}
					else if (activeSpace.SpaceType == SpaceType.Corridor)
					{
						if (activeSpace.Parent == null)
							Corridors.Add(CreateCorridorFromSpace(activeSpace));
					}
				}
		}

		private void DetermineSpaceTypes()
		{
			for (int column = 0; column < NumColumns; column++)
				for (int row = 0; row < NumRows; row++)
				{
					FloorSpace activeSpace = GetFloorSpace(column, row);
					if (activeSpace == null || activeSpace.SpaceType != SpaceType.None)
						continue;

					if (HasThreeAdjacentSpaces(column, row))
						activeSpace.SpaceType = SpaceType.Room;
					else
						activeSpace.SpaceType = SpaceType.Corridor;
				}
		}

		public void PixelsToColumnRow(double x, double y, out int column, out int row)
		{
			column = (int)(x / Tile.Width);
			row = (int)(y / Tile.Height);
		}

		public List<Tile> GetTilesInPixelRect(int left, int top, int width, int height)
		{
			PixelsToColumnRow(left, top, out int leftColumn, out int topRow);
			PixelsToColumnRow(left + width - 1, top + height - 1, out int rightColumn, out int bottomRow);
			List<Tile> results = new List<Tile>();
			foreach (Tile baseSpace in Tiles)
			{
				if (baseSpace.Column >= leftColumn && baseSpace.Column <= rightColumn &&
						baseSpace.Row >= topRow && baseSpace.Row <= bottomRow)
					results.Add(baseSpace);
			}
			return results;
		}

		public List<Tile> GetTilesOutsidePixelRect(int left, int top, int width, int height)
		{
			PixelsToColumnRow(left, top, out int leftColumn, out int topRow);
			PixelsToColumnRow(left + width - 1, top + height - 1, out int rightColumn, out int bottomRow);
			List<Tile> results = new List<Tile>();
			foreach (Tile baseSpace in Tiles)
			{
				if (baseSpace.Column < leftColumn || baseSpace.Column > rightColumn ||
						baseSpace.Row < topRow || baseSpace.Row > bottomRow)
					results.Add(baseSpace);
			}
			return results;
		}

		public void ClearSelection()
		{
			foreach (Tile tile in Tiles)
				tile.Selected = false;
		}

		public bool SelectionExists()
		{
			foreach (Tile tile in Tiles)
				if (tile.Selected)
					return true;
			return false;
		}

		public List<Tile> GetSelection()
		{
			return Tiles.Where(x => x.Selected).ToList();
		}

		public void GetSelectionBoundaries(out int left, out int top, out int right, out int bottom)
		{
			left = int.MaxValue;
			top = int.MaxValue;
			right = 0;
			bottom = 0;
			List<Tile> selection = GetSelection();
			foreach (Tile tile in selection)
			{
				int tileLeft = tile.PixelX;
				int tileTop = tile.PixelY;
				int tileRight = tileLeft + Tile.Width;
				int tileBottom = tileTop + Tile.Height;
				left = Math.Min(tileLeft, left);
				top = Math.Min(tileTop, top);
				right = Math.Max(tileRight, right);
				bottom = Math.Max(tileBottom, bottom);
			}
		}

		public List<Tile> GetAllOtherSpaces(List<Tile> compareSpaces)
		{
			List<Tile> results = new List<Tile>();
			foreach (Tile tile in Tiles)
				if (compareSpaces.IndexOf(tile) < 0)
					results.Add(tile);
			return results;
		}

		public List<Tile> GetAllMatchingTiles<T>() where T: MapRegion
		{
			List<Tile> results = new List<Tile>();
			foreach (Tile tile in Tiles)
				if (tile is FloorSpace floorSpace && floorSpace.Parent is T)
					results.Add(tile);
			return results;
		}
		public bool TileExists(int column, int row)
		{
			return column >= 0 && row >= 0 && column < NumColumns && row < NumRows;
		}

		public void SetWall(int column, int row, WallOrientation wallOrientation, bool isThere)
		{
			column++;  // To allow for -1, -1 coordinates
			row++;
			switch (wallOrientation)
			{
				case WallOrientation.Horizontal:
					if (_allHorizontalWalls[column, row] != isThere)
					{
						_allHorizontalWalls[column, row] = isThere;
						wallsChanged = true;
					}
					break;
				case WallOrientation.Vertical:
					if (_allVerticalWalls[column, row] != isThere)
					{
						_allVerticalWalls[column, row] = isThere;
						wallsChanged = true; 
					}
					break;
			}
		}

		public int WidthPx
		{
			get
			{
				return (rightmostColumnIndex + 1) * Tile.Width;
			}
		}
		public int HeightPx
		{
			get
			{
				return (lastRowIndex + 1) * Tile.Height;
			}
		}

		public List<Tile> Tiles { get; private set; }
		public List<Room> Rooms { get; private set; } = new List<Room>();
		public List<Corridor> Corridors { get; private set; } = new List<Corridor>();
		public Tile[,] AllTiles { get; private set; }
		public int NumColumns { get; set; }
		public int NumRows { get; set; }
		bool[,] _allHorizontalWalls { get; set; }
		bool[,] _allVerticalWalls { get; set; }

		public event EventHandler WallsChanged;

		protected virtual void OnWallsChanged()
		{
			WallsChanged?.Invoke(this, EventArgs.Empty);
		}
		public WallData CollectHorizontalWall(int column, int row)
		{
			WallData result = new WallData(WallOrientation.Horizontal);
			result.StartColumn = column - 1;
			result.StartRow = row;
			int startX = column * Tile.Width;
			int startY = row * Tile.Height;

			while (column < NumColumns && HasHorizontalWall(column + 1, row) && !HasVerticalTouchingWall(column, row))
			{
				column++;
			}
			result.EndColumn = column;
			result.EndRow = row;
			int endX = column * Tile.Width;
			result.X = startX;
			result.Y = startY;
			result.WallLength = endX + Tile.Width - startX;
			return result;
		}
		public WallData CollectVerticalWall(int column, int row)
		{
			WallData result = new WallData(WallOrientation.Vertical);
			result.StartColumn = column;
			result.StartRow = row - 1;
			int startX = column * Tile.Width;
			int startY = row * Tile.Height;

			while (row < NumRows && HasVerticalWall(column, row + 1) && !HasHorizontalTouchingWall(column, row))
			{
				row++;
			}
			result.EndColumn = column;
			result.EndRow = row;
			int endY = row * Tile.Height;
			result.X = startX;
			result.Y = startY;
			result.WallLength = endY + Tile.Height - startY;
			return result;
		}
		bool HasHorizontalWall(int column, int row)
		{
			column++;
			row++;
			if (column < 0 || row < 0 || column > NumColumns || row > NumRows)
				return false;
			return _allHorizontalWalls[column, row];
		}

		bool HasVerticalWall(int column, int row)
		{
			column++;
			row++;
			if (column < 0 || row < 0 || column > NumColumns || row > NumRows)
				return false;
			return _allVerticalWalls[column, row];
		}

		bool HasVerticalTouchingWall(int column, int row)
		{
			return HasVerticalTouchingWallBottom(column, row) || 
						 HasVerticalTouchingWallTop(column, row);
		}

		private bool HasVerticalTouchingWallTop(int column, int row)
		{
			return HasVerticalWall(column, row);
		}

		private bool HasVerticalTouchingWallBottom(int column, int row)
		{
			return HasVerticalWall(column, row + 1);
		}

		bool HasHorizontalTouchingWall(int column, int row)
		{
			return HasHorizontalTouchingWallRight(column, row) || HasHorizontalTouchingWallLeft(column, row);
		}

		private bool HasHorizontalTouchingWallLeft(int column, int row)
		{
			return HasHorizontalWall(column, row);
		}

		private bool HasHorizontalTouchingWallRight(int column, int row)
		{
			return HasHorizontalWall(column + 1, row);
		}

		public bool HasHorizontalWallStart(int column, int row)
		{
			if (!HasHorizontalWall(column, row))
				return false;
			if (HasHorizontalWall(column - 1, row))
			{
				return HasVerticalTouchingWall(column - 1, row);
			}
			return true;
		}
		public bool HasVerticalWallStart(int column, int row)
		{
			if (!HasVerticalWall(column, row))
				return false;
			if (HasVerticalWall(column, row - 1))
			{
				return HasHorizontalTouchingWall(column, row - 1);
			}
			return true;
		}

		bool wallsChanged;
		public void BeginWallUpdate()
		{
			if (wallChangeCount == 0)
				wallsChanged = false;
			wallChangeCount++;
		}
		public void EndWallUpdate()
		{
			wallChangeCount--;
			if (wallChangeCount == 0 && wallsChanged)
			{
				wallsChanged = false;
				OnWallsChanged();
			}
		}
		public EndCapKind GetEndCapKind(int column, int row)
		{
			bool wallLeft = HasHorizontalTouchingWallLeft(column, row);
			bool wallRight = HasHorizontalTouchingWallRight(column, row);
			bool wallTop = HasVerticalTouchingWallTop(column, row);
			bool wallBottom = HasVerticalTouchingWallBottom(column, row);
			int count = 0;
			if (wallLeft)
				count++;
			if (wallTop)
				count++;
			if (wallRight)
				count++;
			if (wallBottom)
				count++;

			if (count == 4)
				return EndCapKind.FourWayIntersection;

			if (count == 0)
				return EndCapKind.None;

			if (count == 1)
			{
				if (wallLeft)
					return EndCapKind.Right;
				if (wallTop)
					return EndCapKind.Bottom;
				if (wallRight)
					return EndCapKind.Left;
				if (wallBottom)
					return EndCapKind.Top;

				return EndCapKind.None;
			}

			if (count == 2) // Corners
			{
				if (wallLeft && wallTop)
					return EndCapKind.BottomRightCorner;
				if (wallTop && wallRight)
					return EndCapKind.BottomLeftCorner;
				if (wallRight && wallBottom)
					return EndCapKind.TopLeftCorner;
				if (wallBottom && wallLeft)
					return EndCapKind.TopRightCorner;

				return EndCapKind.None;
			}

			// Tees...
			if (!wallLeft)
				return EndCapKind.LeftTee;
			if (!wallTop)
				return EndCapKind.TopTee;
			if (!wallRight)
				return EndCapKind.RightTee;
			if (!wallBottom)
				return EndCapKind.BottomTee;

			return EndCapKind.None;
		}

		public VoidCornerKind GetVoidKind(int column, int row)
		{
			bool hasUpperLeftTile = FloorSpaceExists(column, row);
			bool hasUpperRightTile = FloorSpaceExists(column + 1, row);
			bool hasLowerLeftTile = FloorSpaceExists(column, row + 1);
			bool hasLowerRightTile = FloorSpaceExists(column + 1, row + 1);
			int tileCount = 0;
			if (hasUpperLeftTile)
				tileCount++;
			if (hasUpperRightTile)
				tileCount++;
			if (hasLowerLeftTile)
				tileCount++;
			if (hasLowerRightTile)
				tileCount++;

			if (tileCount == 3)
			{
				if (!hasLowerRightTile)
					return VoidCornerKind.InsideTopLeft;
				if (!hasUpperRightTile)
					return VoidCornerKind.InsideBottomLeft;
				if (!hasLowerLeftTile)
					return VoidCornerKind.InsideTopRight;
				if (!hasUpperLeftTile)
					return VoidCornerKind.InsideBottomRight;
			}

			if (tileCount == 1)
			{
				if (hasLowerRightTile)
					return VoidCornerKind.OutsideTopLeft;
				if (hasUpperRightTile)
					return VoidCornerKind.OutsideBottomLeft;
				if (hasLowerLeftTile)
					return VoidCornerKind.OutsideTopRight;
				if (hasUpperLeftTile)
					return VoidCornerKind.OutsideBottomRight;
			}

			if (tileCount == 2)
			{
				if (hasLowerRightTile && hasUpperRightTile)
					return VoidCornerKind.TLeft;
				if (hasUpperLeftTile && hasUpperRightTile)
					return VoidCornerKind.TBottom;
				if (hasUpperLeftTile && hasLowerLeftTile)
					return VoidCornerKind.TRight;
				if (hasLowerRightTile && hasLowerLeftTile)
					return VoidCornerKind.TTop;
			}

			return VoidCornerKind.None;
		}
		public bool VoidIsLeft(int column, int row)
		{
			return !FloorSpaceExists(column, row + 1);
		}
		public bool VoidIsRight(int column, int row)
		{
			return !FloorSpaceExists(column + 1, row + 1);
		}
		public bool VoidIsAbove(int column, int row)
		{
			return !FloorSpaceExists(column + 1, row);
		}
		public bool VoidIsBelow(int column, int row)
		{
			return !FloorSpaceExists(column + 1, row + 1);
		}
	}
}
