﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using CardMaker;
using Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardMaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class CardMakerMain : Window
	{
		const string assetsFolder = @"D:\Dropbox\DragonHumpers\Monetization\StreamLoots\Card Factory\Assets";

		bool changingDataInternally;
		public CardData CardData { get; set; }
		Deck activeDeck;
		public Deck ActiveDeck
		{
			get => activeDeck; 
			set
			{
				if (activeDeck == value)
					return;
				activeDeck = value;
				SelectDeck(activeDeck);
			}
		}
		LayerTextOptions activeLayerTextOptions;
		Card activeCard;
		public Card ActiveCard
		{
			get => activeCard;
			set
			{
				if (activeCard == value)
					return;
				activeCard = value;
				SelectCard(activeCard);
			}
		}
		Field activeField;
		public Field ActiveField
		{
			get => activeField;
			set
			{
				if (activeField == value)
					return;
				activeField = value;
				SelectField(activeField);
			}
		}

		void SelectDeck(Deck deck)
		{
			lbDecks.SelectedItem = deck;
			lbCards.ItemsSource = deck?.Cards;
			UpdateCardsLabel(deck);
		}
		
		void UpdateCardsLabel(Deck deck)
		{
			tbCards.Text = deck == null ? "" : $"Cards in {deck.Name}:";
		}

		void SelectCard(Card card)
		{
			lbCards.SelectedItem = card;
			if (card == null)
				return;
			card.BeginUpdate();
			try
			{
				LoadNewStyle(card.StylePath);
			}
			finally
			{
				card.EndUpdate();
			}
		}

		void SelectField(Field field)
		{
			lbFields.SelectedItem = field;
		}

		public CardMakerMain()
		{
			InitializeComponent();
			CardData = new CardData();
			CardData.LoadData();

			if (CardData.AllKnownDecks != null && CardData.AllKnownDecks.Count > 0)
				SetActiveDeck(CardData.AllKnownDecks[0]);
			else
				SetActiveDeck(null);

			lbDecks.ItemsSource = CardData.AllKnownDecks;
		}



		void ShowStatus(string message)
		{
			statusBar.Visibility = Visibility.Visible;
			sbMessage.Content = message;
		}

		private void btnCloseStatusMessage_Click(object sender, RoutedEventArgs e)
		{
			statusBar.Visibility = Visibility.Hidden;
		}

		private void TestStatusBarMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ShowStatus("Menu item clicked!");
		}

		private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
		{
		}

		void SelectDeckByIndex(int index)
		{
			ActiveDeck = null;
			SelectByIndex(lbDecks, index);
		}

		private void SelectByIndex(ListBox listBox, int index)
		{
			if (index >= listBox.Items.Count)
				index = listBox.Items.Count - 1;

			if (index < 0)
				return;
			listBox.SelectedItem = listBox.Items[index];
		}

		void SelectCardByIndex(int index)
		{
			ActiveCard = null;
			SelectByIndex(lbCards, index);
		}

		void SelectFieldByIndex(int index)
		{
			ActiveField = null;
			SelectByIndex(lbFields, index);
		}

		void SetActiveLayerTextOptions(LayerTextOptions layerTextOptions)
		{
			activeLayerTextOptions = layerTextOptions;
			grdCardText.DataContext = layerTextOptions;
		}

		void SetActiveLayerTextOptionsForCard(Card card)
		{
			SetActiveLayerTextOptions(CardData.GetLayerTextOptions(card?.StylePath));
		}

		void SetActiveCard(Card card)
		{
			changingDataInternally = true;
			try
			{
				ActiveCard = card;
				
				lbCards.SelectedItem = card;
				spSelectedCard.DataContext = card;
				SetActiveFields(card?.Fields);
				if (card == null)
					SetActiveLayerTextOptions(null);
				else
				{
					if (activeLayerTextOptions != null)
					{
						ActiveCard.BeginUpdate();
						try
						{
							ActiveCard.FontBrush = activeLayerTextOptions.FontBrush;
						}
						finally
						{
							ActiveCard.EndUpdate();
						}
					}
					SetActiveLayerTextOptionsForCard(card);
				}

				tbItemName.DataContext = card;
				bool isValidCard = card != null;
				btnDeleteCard.IsEnabled = isValidCard;
				spSelectedCard.Visibility = isValidCard ? Visibility.Visible : Visibility.Collapsed;
			}
			finally
			{
				changingDataInternally = false;
			}
		}

		void SetActiveDeck(Deck deck)
		{
			ActiveDeck = deck;
			if (deck == null)
				return;

			changingDataInternally = true;
			try
			{
				spActiveDeck.DataContext = deck;
				if (deck.Cards.Count == 0)
					SetActiveCard(null);
				else
					SetActiveCard(deck.Cards[0]);
			}
			finally
			{
				changingDataInternally = false;
			}
		}

		private void btnDeleteDeck_Click(object sender, RoutedEventArgs e)
		{
			if (ActiveDeck == null)
				return;

			if (ActiveDeck.Cards == null || ActiveDeck.Cards.Count > 0)
			{
				ShowStatus("You can only delete empty decks! Delete all cards first!");
				return;
			}

			if (MessageBox.Show($"Delete deck \"{ActiveDeck.Name}\"?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				return;

			int lastSelectedIndex = lbDecks.Items.IndexOf(ActiveDeck);
			CardData.Delete(ActiveDeck);
			int newIndexToSelect = lastSelectedIndex;
			SelectDeckByIndex(newIndexToSelect);
		}

		private void lbDecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SetActiveDeck(lbDecks.SelectedItem as Deck);
		}

		private void btnAddDeck_Click(object sender, RoutedEventArgs e)
		{
			ActiveDeck = CardData.AddDeck();
		}
		
		private void btnAddCard_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ActiveCard = CardData.AddCard(ActiveDeck);
				tbxCardName.SelectAll();
				tbxCardName.Focus();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Exception");
			}
		}

		private void btnDeleteCard_Click(object sender, RoutedEventArgs e)
		{
			if (ActiveCard == null)
				return;

			if (ActiveCard.Fields == null || ActiveCard.Fields.Count > 0)
			{
				ShowStatus("You can only delete cards with no fields! Delete all fields first!");
				return;
			}

			if (MessageBox.Show($"Delete card \"{ActiveCard.Name}\"?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				return;

			int lastSelectedIndex = lbCards.Items.IndexOf(ActiveCard);
			CardData.Delete(ActiveCard);
			int newIndexToSelect = lastSelectedIndex;
			SelectCardByIndex(newIndexToSelect);
		}

		void SetActiveFields(ObservableCollection<Field> fields)
		{
			lbFields.ItemsSource = fields;
			if (fields == null || fields.Count == 0)
				SetActiveField(null);
			else
				SetActiveField(fields[0]);
		}

		private void lbCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			changingDataInternally = true;
			try
			{
				SetActiveCard(lbCards.SelectedItem as Card);
			}
			finally
			{
				changingDataInternally = false;
			}
		}

		private void lbCardLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			sliderOffsetX.DataContext = cardImageLayer.Details;
			sliderOffsetY.DataContext = cardImageLayer.Details;
			sliderHorizontalStretch.DataContext = cardImageLayer.Details;
			sliderVerticalStretch.DataContext = cardImageLayer.Details;
			sliderHue.DataContext = cardImageLayer.Details;
			sliderLightness.DataContext = cardImageLayer.Details;
			sliderSaturation.DataContext = cardImageLayer.Details;
			sliderContrast.DataContext = cardImageLayer.Details;
			tbLayerName.DataContext = cardImageLayer.Details;

			btnResetOffsets.DataContext = cardImageLayer;
			btnResetScale.DataContext = cardImageLayer;
			btnResetHue.DataContext = cardImageLayer;
			btnResetLightness.DataContext = cardImageLayer;
			btnResetSaturation.DataContext = cardImageLayer;
			btnResetContrast.DataContext = cardImageLayer;

			if (cardImageLayer.Alternates != null && cardImageLayer.Alternates.CardLayers.Count > 0)
			{
				lbAlternates.Visibility = Visibility.Visible;
				lbAlternates.ItemsSource = cardImageLayer.Alternates.CardLayers;
			}
			else
				lbAlternates.Visibility = Visibility.Collapsed;

		}

		private void sliderSaturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}

		private void sliderHue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}

		private void sliderOffsetY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}

		private void sliderOffsetX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}

		private void tbxAdditionalInstructions_TextChanged(object sender, TextChangedEventArgs e)
		{

		}

		private void tbxDescription_TextChanged(object sender, TextChangedEventArgs e)
		{

		}

		private void tbxFragmentsRequired_TextChanged(object sender, TextChangedEventArgs e)
		{

		}

		private void tbxCooldown_TextChanged(object sender, TextChangedEventArgs e)
		{

		}

		void SetActiveField(Field field)
		{
			changingDataInternally = true;
			try
			{
				ActiveField = field;
				lbFields.SelectedItem = field;
				spActiveField.DataContext = field;
				bool isValidField = field != null;
				btnDeleteField.IsEnabled = isValidField;
				spActiveField.Visibility = isValidField ? Visibility.Visible : Visibility.Collapsed;
			}
			finally
			{
				changingDataInternally = false;
			}
		}

		private void lbFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SetActiveField(lbFields.SelectedItem as Field);
		}

		private void btnAddField_Click(object sender, RoutedEventArgs e)
		{
			SetActiveField(CardData.AddField(ActiveCard));
			tbxFieldName.Focus();
			tbxFieldName.SelectAll();
		}

		private void btnDeleteField_Click(object sender, RoutedEventArgs e)
		{
			if (ActiveField == null)
				return;

			if (MessageBox.Show($"Delete field \"{ActiveField.Name}\"?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				return;

			int lastSelectedIndex = lbFields.Items.IndexOf(ActiveField);
			CardData.Delete(ActiveField);
			int newIndexToSelect = lastSelectedIndex;
			SelectFieldByIndex(newIndexToSelect);
		}

		private void cmMoveCardTo_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			// Delete this.
		}

		private void cmMoveCardTo_SubmenuOpened(object sender, RoutedEventArgs e)
		{
			cmMoveCardTo.Items.Clear();
			foreach (Deck deck in CardData.AllKnownDecks)
				if (deck != ActiveDeck)
				{
					MenuItem menuItem = new MenuItem() { Header = deck.Name, Tag = deck };
					cmMoveCardTo.Items.Add(menuItem);
					menuItem.Click += MoveCardToMenuItem_Click;
				}

		}

		private void MoveCardToMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is MenuItem menuItem))
				return;
			if (!(menuItem.Tag is Deck deck))
				return;

			CardData.MoveCardToDeck(ActiveCard, deck);
		}

		bool ignoreFirstDeckNameChange = true;
		private void tbxDeckName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (changingDataInternally)
				return;
			if (ignoreFirstDeckNameChange)
			{
				ignoreFirstDeckNameChange = false;
				return;
			}
			ActiveDeck.IsDirty = true;
			lbDecks.Items.Refresh();
			UpdateCardsLabel(ActiveDeck);
		}

		private void tbxCardName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (changingDataInternally)
				return;
			if (ActiveCard == null)
				return;
			ActiveCard.IsDirty = true;
			if (ActiveCard.ParentDeck != null)
				ActiveCard.ParentDeck.IsDirty = true;
			lbCards.Items.Refresh();
		}

		private void tbxFieldName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (changingDataInternally)
				return;
			if (ActiveField == null)
				return;
			ActiveField.IsDirty = true;
			if (ActiveField.ParentCard != null)
			{
				ActiveField.ParentCard.IsDirty = true;
				if (ActiveField.ParentCard.ParentDeck != null)
					ActiveField.ParentCard.ParentDeck.IsDirty = true;
			}
			lbFields.Items.Refresh();
		}

		void AddCardStyleMenuItems(ItemCollection items, string[] directories)
		{
			foreach (string directory in directories)
			{
				string folderName = System.IO.Path.GetFileName(directory);
				if (folderName.StartsWith("Shared"))
					continue;
				CardStyleMenuItem newMenuItem = new CardStyleMenuItem() { Header = folderName };
				newMenuItem.StylePath = directory.Substring(assetsFolder.Length + 1);
				items.Add(newMenuItem);
				string[] subDirectories = Directory.GetDirectories(directory);
				if (subDirectories.Length > 0)
					AddCardStyleMenuItems(newMenuItem.Items, subDirectories);
				else
					newMenuItem.Click += ChangeCardStyleItem_Click;
			}
		}

		private void ChangeCardStyleItem_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is CardStyleMenuItem cardStyleMenuItem))
				return;

			LoadNewStyle(cardStyleMenuItem.StylePath);
		}

		private void btnCardStylePicker_MouseDown(object sender, MouseButtonEventArgs e)
		{
			string[] directories = Directory.GetDirectories(assetsFolder);
			mnuCardStyle.Items.Clear();
			AddCardStyleMenuItems(mnuCardStyle.Items, directories);
		}

		CardLayerManager cardLayerManager = new CardLayerManager();

		void DeleteAllImages(Canvas canvas)
		{
			for (int i = canvas.Children.Count - 1; i >= 0; i--)
			{
				if (canvas.Children[i] is Image)
					canvas.Children.RemoveAt(i);
			}
		}

		void LoadNewStyle(string stylePath)
		{
			if (string.IsNullOrWhiteSpace(stylePath))
			{
				btnCardStylePicker.Content = "Card Style";
				return;
			}

			btnCardStylePicker.Content = $"Card Style: {stylePath}";

			cardLayerManager.Clear();

			if (ActiveCard != null)
				ActiveCard.StylePath = stylePath;
			DeleteAllImages(cvsLayers);
			AddBackgroundLayers(stylePath);
			AddCoreLayers(stylePath);
			AddForegroundLayers(stylePath);
			cardLayerManager.AddLayersToCanvas(cvsLayers);
			cardLayerManager.SortByLayerIndex();
			MoveTextLayerToTop();
			lbCardLayers.ItemsSource = cardLayerManager.CardLayers;
			if (cardLayerManager.CardLayers.Count > 0)
				lbCardLayers.SelectedItem = cardLayerManager.CardLayers[0];
			SetActiveLayerTextOptionsForCard(ActiveCard);
			LayerTextOptions layerTextOptions = CardData.GetLayerTextOptions(stylePath);
			if (ActiveCard != null && layerTextOptions != null)
			{
				ActiveCard.BeginUpdate();
				try
				{
					ActiveCard.FontBrush = layerTextOptions.FontBrush;
				}
				finally
				{
					ActiveCard.EndUpdate();
				}
				activeLayerTextOptions = layerTextOptions;
			}
		}

		private CardImageLayer AddLayer(string pngFile)
		{
			CardImageLayer cardImageLayer = new CardImageLayer(pngFile);
			if (cardImageLayer.LayerName == "Placeholder" && !string.IsNullOrWhiteSpace(ActiveCard.Placeholder))
			{
				cardImageLayer.PngFile = ActiveCard.Placeholder;
			}
			cardImageLayer.NeedToReloadImage += CardImageLayer_NeedToReloadImage;
			cardImageLayer.Details = ActiveCard.GetLayerDetails(cardImageLayer.LayerName);
			cardLayerManager.Add(cardImageLayer);
			return cardImageLayer;
		}

		private void AddCoreLayers(string stylePath)
		{
			string[] pngFiles = Directory.GetFiles(System.IO.Path.Combine(assetsFolder, stylePath), "*.png");
			foreach (string pngFile in pngFiles)
				AddLayer(pngFile);
		}

		private void AddBackgroundLayers(string stylePath)
		{
			const string backgroundFolderName = "Shared Back Layer Adornments";
			string basePath = System.IO.Path.GetDirectoryName(stylePath);
			string backFolder = System.IO.Path.Combine(assetsFolder, basePath, backgroundFolderName);
			if (!Directory.Exists(backFolder))
				return;
			string[] pngFiles = Directory.GetFiles(backFolder, "*.png");
			foreach (string pngFile in pngFiles)
				AddLayer(pngFile).Index -= 100;
		}

		private void AddForegroundLayers(string stylePath)
		{
			const string foregroundFolderName = "Shared Top Layer Adornments";
			string basePath = System.IO.Path.GetDirectoryName(stylePath);
			string foregroundFolder = System.IO.Path.Combine(assetsFolder, basePath, foregroundFolderName);
			if (!Directory.Exists(foregroundFolder))
				return;
			string[] pngFiles = Directory.GetFiles(foregroundFolder, "*.png");
			foreach (string pngFile in pngFiles)
				AddLayer(pngFile).Index += 100;
		}

		private void CardImageLayer_NeedToReloadImage(object sender, EventArgs e)
		{
			if (!(sender is CardImageLayer cardImageLayer))
				return;
			int index = GetIndexOfChild(cardImageLayer);
			if (index == -1)
				return;

			cvsLayers.Children.RemoveAt(index);
			Image image = cardImageLayer.CreateImage();
			cvsLayers.Children.Insert(index, image);
		}

		private int GetIndexOfChild(object sender)
		{
			int index = -1;
			foreach (UIElement uIElement in cvsLayers.Children)
			{
				if (uIElement is FrameworkElement frameworkElement)
				{
					if (frameworkElement.Tag == sender)
					{
						index = cvsLayers.Children.IndexOf(frameworkElement);
						break;
					}
				}
			}

			return index;
		}

		private void MoveTextLayerToTop()
		{
			Panel.SetZIndex(grdCardText, 200);
		}

		private void btnResetOffsets_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.OffsetX = 0;
			cardImageLayer.Details.OffsetY = 0;
		}

		private void btnResetScale_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.ScaleX = 1;
			cardImageLayer.Details.ScaleY = 1;
		}

		private void btnResetHue_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.Hue = 0;
		}

		private void btnResetSaturation_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.Sat = 0;
		}

		private void btnResetLightness_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.Light = 0;
		}

		private void btnResetContrast_Click(object sender, RoutedEventArgs e)
		{
			if (!(lbCardLayers.SelectedItem is CardImageLayer cardImageLayer))
				return;
			cardImageLayer.Details.Contrast = 0;
		}

		private void Save()
		{
			ShowStatus("Saving...");
			statusBar.Refresh();
			CardData.Save();
			statusBar.Visibility = Visibility.Collapsed;
		}

		private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Save();
		}

		private void ToggleVisibility_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!(sender is Viewbox viewbox))
				return;

			if (!(viewbox.DataContext is CardImageLayer cardImageLayer))
				return;

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					cardLayerManager.ShowAllLayers();
				else
					cardLayerManager.HideAllLayersExcept(cardImageLayer);
			else
				cardImageLayer.ToggleVisibility();
		}

		private void lbAlternates_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lbAlternates.SelectedItem is CardImageLayer newImageLayer)
			{
				if (lbCardLayers.SelectedItem is CardImageLayer selectedLayer)
				{
					cardLayerManager.Replace(selectedLayer, newImageLayer, cvsLayers);
					// TODO: **Replace** the selectedLayer in cardLayerManager.CardLayers
					// TODO: **Replace** the image on the canvas with the new image.
					lbCardLayers.SelectedItem = newImageLayer;
				}
			}
		}

		void AddSpellMenuItems(ItemCollection items, string[] files)
		{
			foreach (string file in files)
			{
				MenuItem menuItem = new MenuItem() { Header = System.IO.Path.GetFileNameWithoutExtension(file), Tag = file };
				menuItem.Click += AddSpellImageItem_Click;
				items.Add(menuItem);
			}
		}

		private void AddSpellImageItem_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is MenuItem menuItem))
				return;

			if (!(menuItem.Tag is string fileName && fileName != null))
				return;

			string spellName = System.IO.Path.GetFileNameWithoutExtension(fileName);
			if (ActiveCard != null)
			{
				ActiveCard.Text = spellName;
				ActiveCard.Placeholder = fileName;
				cardLayerManager.ReplaceImageInPlaceHolder(fileName, cvsLayers);
			}
		}

		private void btnLoadSpellImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			const string spellFolder = @"D:\Dropbox\DX\Twitch\CodeRushed\MrAnnouncerBot\OverlayManager\wwwroot\GameDev\Assets\DragonH\Scroll\Spells\Icons";
			string[] files = Directory.GetFiles(spellFolder, "*.png");
			mnuPickSpell.Items.Clear();
			AddSpellMenuItems(mnuPickSpell.Items, files);
		}
	}
}
