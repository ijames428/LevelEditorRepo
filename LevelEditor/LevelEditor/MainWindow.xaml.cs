using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;

namespace LevelEditor
{
	public partial class MainWindow : Window
	{
		bool isDataDirty = false;
		string levelEditorFileName = "level_editor_settings.sav";
		SerializedSettings Settings = new SerializedSettings();

		SolidColorBrush TransformingColor = Brushes.LightGreen;
		SolidColorBrush SelectedColor = Brushes.Green;
		SolidColorBrush DormantColor = Brushes.LightBlue;
		
		const int OBJECT_SELECTION_RECTANGLE = 0;
		const int OBJECT_SELECTION_TRIANGLE = 1;
		const int OBJECT_SELECTION_DOOR = 2;
		const int OBJECT_SELECTION_TRIGGER = 3;
		const int OBJECT_SELECTION_RAIL = 4;
		const int OBJECT_SELECTION_UNIT_STARTING_INDEX = 5;

		Point mouse_pressed_pos = new Point();

		System.Windows.Threading.DispatcherTimer updateTimer = new System.Windows.Threading.DispatcherTimer();

		Rectangle rect_getting_drawn = null;
		Polygon triangle_getting_drawn = null;
		Line line_getting_drawn = null;

		//Polygon triangle_getting_made = null;
		Dictionary<string, Polygon> triangles = new Dictionary<string, Polygon>();
		List<SerializedTriangle> sTris = new List<SerializedTriangle>();
		Polygon selected_triangle = null;
		string selected_triangle_name = "";

		Rectangle selected_rectangle = null;
		Dictionary<string, Rectangle> rectangles = new Dictionary<string, Rectangle>();
		List<SerializedRectangle> sRects = new List<SerializedRectangle>();
		string selected_rect_name = "";

		Rectangle selected_door = null;
		Dictionary<string, Rectangle> door_rects = new Dictionary<string, Rectangle>();
		List<SerializedDoor> sDoors = new List<SerializedDoor>();
		string selected_door_name = "";

		SerializedPlayer sPlayer = new SerializedPlayer();
		Rectangle playerRectangle = null;
		bool playerRectSelected = false;

		List<string> ImportedBestiariesFilePaths = new List<string>();
		List<Bestiary> Bestiaries = new List<Bestiary>();

		//Unit selected_unit = null;
		Rectangle selected_unit_rectangle = null;
		Dictionary<string, Rectangle> unit_rects = new Dictionary<string, Rectangle>();
		List<SerializedUnit> sUnits = new List<SerializedUnit>();
		string selected_unit_name = "";

		Rectangle selected_trigger = null;
		Dictionary<string, Rectangle> trigger_rects = new Dictionary<string, Rectangle>();
		List<SerializedTrigger> sTriggers = new List<SerializedTrigger>();
		string selected_trigger_name = "";

		Line selected_rail = null;
		Dictionary<string, Line> rail_lines = new Dictionary<string, Line>();
		List<SerializedRail> sRails = new List<SerializedRail>();
		string selected_rail_name = "";

		ArtImage selected_image = null;
		List<BitmapImage> bitmap = new List<BitmapImage>();
		List<ArtImage> artImages = new List<ArtImage>();
		string selected_image_name = "";

		List<Zone> zones = new List<Zone>();

		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
			Show();

			updateTimer.Tick += Update;
			updateTimer.Interval = TimeSpan.FromSeconds(1.0 / 30.0);
			updateTimer.Start();

			while (!File.Exists(levelEditorFileName))
			{
				SetRootDirectory();

				if (!File.Exists(levelEditorFileName))
				{
					string msg = "You must select a Root Folder in order to proceed.  Do you wish to close the program?";
					MessageBoxResult yesOrNoResult =
					  MessageBox.Show(
						msg,
						"Data App",
						MessageBoxButton.YesNo,
						MessageBoxImage.Warning);
					if (yesOrNoResult == MessageBoxResult.Yes || yesOrNoResult == MessageBoxResult.OK)
					{
						Close();
						return;
					}
				}
			}

			string result = "";
			System.IO.StreamReader sr = new System.IO.StreamReader(levelEditorFileName);
			result = sr.ReadToEnd();
			sr.Close();

			Settings = JsonConvert.DeserializeObject<SerializedSettings>(result);

			zones.Add(new Zone("Zone 1"));

			CreatePlayer();
		}

		private void Update(object sender, EventArgs e)
		{
			if (xTextBox.IsFocused || yTextBox.IsFocused || wTextBox.IsFocused || hTextBox.IsFocused || activityTextBox.IsFocused || velocityXTextBox.IsFocused || velocityYTextBox.IsFocused || PassThroughCheckBox.IsFocused)
			{
				if (playerRectSelected)
				{
					float.TryParse(xTextBox.Text, out sPlayer.x);
					float.TryParse(yTextBox.Text, out sPlayer.y);
					float.TryParse(wTextBox.Text, out sPlayer.width);
					float.TryParse(hTextBox.Text, out sPlayer.height);

					playerRectangle.Width = sPlayer.width;
					playerRectangle.Height = sPlayer.height;

					Canvas.SetLeft(playerRectangle, sPlayer.x);
					Canvas.SetTop(playerRectangle, sPlayer.y);
				}

				SerializedRectangle sRect = GetSerializedRect(selected_rect_name);
				if (sRect != null)
				{
					float.TryParse(xTextBox.Text, out sRect.x);
					float.TryParse(yTextBox.Text, out sRect.y);
					float.TryParse(wTextBox.Text, out sRect.width);
					float.TryParse(hTextBox.Text, out sRect.height);
					float.TryParse(velocityXTextBox.Text, out sRect.vel_x);
					float.TryParse(velocityYTextBox.Text, out sRect.vel_y);

					sRect.pass_through = PassThroughCheckBox.IsChecked.HasValue ? PassThroughCheckBox.IsChecked.Value : false;

					selected_rectangle.Width = sRect.width;
					selected_rectangle.Height = sRect.height;

					Canvas.SetLeft(selected_rectangle, sRect.x);
					Canvas.SetTop(selected_rectangle, sRect.y);
				}

				SerializedDoor sDoor = GetSerializedDoor(selected_door_name);
				if (sDoor != null)
				{
					float.TryParse(xTextBox.Text, out sDoor.x);
					float.TryParse(yTextBox.Text, out sDoor.y);
					sDoor.activator = doorActivatorTextBox.Text;

					Canvas.SetLeft(selected_door, sDoor.x);
					Canvas.SetTop(selected_door, sDoor.y);
				}

				SerializedUnit sUnit = GetSerializedUnit(selected_unit_name);
				if (sUnit != null)
				{
					float.TryParse(xTextBox.Text, out sUnit.LevelLocationX);
					float.TryParse(yTextBox.Text, out sUnit.LevelLocationY);
					sUnit.activity = activityTextBox.Text;

					Canvas.SetLeft(selected_unit_rectangle, sUnit.LevelLocationX);
					Canvas.SetTop(selected_unit_rectangle, sUnit.LevelLocationY);
				}

				SerializedTrigger sTrigger = GetSerializedTrigger(selected_trigger_name);
				if (sTrigger != null)
				{
					float.TryParse(xTextBox.Text, out sTrigger.x);
					float.TryParse(yTextBox.Text, out sTrigger.y);
					float.TryParse(wTextBox.Text, out sTrigger.width);
					float.TryParse(hTextBox.Text, out sTrigger.height);
					sTrigger.activity = activityTextBox.Text;

					selected_trigger.Width = sTrigger.width;
					selected_trigger.Height = sTrigger.height;

					Canvas.SetLeft(selected_trigger, sTrigger.x);
					Canvas.SetTop(selected_trigger, sTrigger.y);
				}

				ArtImage image = GetArtImage(selected_image_name);
				if (image != null && image.IsStandalone)
				{
					float.TryParse(xTextBox.Text, out image.x);
					float.TryParse(yTextBox.Text, out image.y);
					wTextBox.Text = "";
					hTextBox.Text = "";

					Canvas.SetLeft(image.image, image.x);
					Canvas.SetTop(image.image, image.y);
				}
			}
			else if (IsInteractableCheckBox.IsFocused || IsDestructibleCheckBox.IsFocused)
			{
				SerializedUnit sUnit = GetSerializedUnit(selected_unit_name);
				if (sUnit != null)
				{
					sUnit.IsInteractable = IsInteractableCheckBox.IsChecked.HasValue ? IsInteractableCheckBox.IsChecked.Value : false;
					sUnit.IsDestructible = IsDestructibleCheckBox.IsChecked.HasValue ? IsDestructibleCheckBox.IsChecked.Value : false;
				}
			}
			else if (p1xTextBox.IsFocused || p1yTextBox.IsFocused || p2xTextBox.IsFocused || p2yTextBox.IsFocused)
			{
				SerializedTriangle sTri = GetSerializedTriangle(selected_triangle_name);
				if (sTri != null)
				{
					float newP1X = 0.0f;
					float newP1Y = 0.0f;
					float newP2X = 0.0f;
					float newP2Y = 0.0f;

					float.TryParse(p1xTextBox.Text, out newP1X);
					float.TryParse(p1yTextBox.Text, out newP1Y);
					float.TryParse(p2xTextBox.Text, out newP2X);
					float.TryParse(p2yTextBox.Text, out newP2Y);

					//sTri.right_angle_above_line = TriangleCheckBox.IsChecked.HasValue ? TriangleCheckBox.IsChecked.Value : false;

					selected_triangle.Points[0] = new Point(newP1X, newP1Y);
					selected_triangle.Points[2] = new Point(newP2X, newP2Y);

					if (sTri.right_angle_above_line)
					{
						selected_triangle.Points[1] = new Point(selected_triangle.Points[2].X, selected_triangle.Points[0].Y);
					}
					else
					{
						selected_triangle.Points[1] = new Point(selected_triangle.Points[0].X, selected_triangle.Points[2].Y);
					}

					sTri.points = selected_triangle.Points;

					Canvas.SetLeft(selected_triangle, 0.0f);
					Canvas.SetTop(selected_triangle, 0.0f);
				}
				
				SerializedRail sRail = GetSerializedRail(selected_rail_name);
				if (sRail != null)
				{
					float newP1X = 0.0f;
					float newP1Y = 0.0f;
					float newP2X = 0.0f;
					float newP2Y = 0.0f;

					float.TryParse(p1xTextBox.Text, out newP1X);
					float.TryParse(p1yTextBox.Text, out newP1Y);
					float.TryParse(p2xTextBox.Text, out newP2X);
					float.TryParse(p2yTextBox.Text, out newP2Y);

					selected_rail.X1 = sRail.x = newP1X;
					selected_rail.Y1 = sRail.y = newP1Y;
					selected_rail.X2 = sRail.x_prime = newP2X;
					selected_rail.Y2 = sRail.y_prime = newP2Y;

					Canvas.SetLeft(selected_rail, 0.0f);
					Canvas.SetTop(selected_rail, 0.0f);
				}
			}
			else if (xZoneTextBox.IsFocused || yZoneTextBox.IsFocused)
			{
				Zone zone = GetCurrentSelectedZone();
				if (zone != null)
				{
					if (xZoneTextBox.IsFocused)
					{
						float new_x;
						float.TryParse(xZoneTextBox.Text, out new_x);
						if (zone.x != new_x)
						{
							zone.x = new_x;
						}
					}
					else if (yZoneTextBox.IsFocused)
					{
						float new_y;
						float.TryParse(yZoneTextBox.Text, out new_y);
						if (zone.y != new_y)
						{
							zone.y = new_y;
						}
					}
				}
			}
		}

		public void CanvasDown(object sender, MouseButtonEventArgs e)
		{
			Deselect();

			mouse_pressed_pos = e.GetPosition(MyCanvas);

			if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_RECTANGLE || ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_TRIGGER)
			{
				rect_getting_drawn = new Rectangle
				{
					Stroke = TransformingColor,
					StrokeThickness = 1
				};
				Canvas.SetLeft(rect_getting_drawn, mouse_pressed_pos.X);
				Canvas.SetTop(rect_getting_drawn, mouse_pressed_pos.Y);
				MyCanvas.Children.Add(rect_getting_drawn);
			}
			else if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_TRIANGLE)
			{
				triangle_getting_drawn = new Polygon();
				triangle_getting_drawn.Stroke = TransformingColor;
				triangle_getting_drawn.StrokeThickness = 1;
				triangle_getting_drawn.HorizontalAlignment = HorizontalAlignment.Center;
				triangle_getting_drawn.VerticalAlignment = VerticalAlignment.Center;

				triangle_getting_drawn.Points = new PointCollection() { mouse_pressed_pos, mouse_pressed_pos, mouse_pressed_pos };

				Canvas.SetLeft(triangle_getting_drawn, mouse_pressed_pos.X);
				Canvas.SetTop(triangle_getting_drawn, mouse_pressed_pos.Y);
				MyCanvas.Children.Add(triangle_getting_drawn);
			}
			else if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_RAIL)
			{
				line_getting_drawn = new Line();
				line_getting_drawn.Stroke = TransformingColor;
				line_getting_drawn.StrokeThickness = 1;
				line_getting_drawn.HorizontalAlignment = HorizontalAlignment.Center;
				line_getting_drawn.VerticalAlignment = VerticalAlignment.Center;

				line_getting_drawn.X1 = mouse_pressed_pos.X;
				line_getting_drawn.Y1 = mouse_pressed_pos.Y;

				Canvas.SetLeft(line_getting_drawn, mouse_pressed_pos.X);
				Canvas.SetTop(line_getting_drawn, mouse_pressed_pos.Y);
				MyCanvas.Children.Add(line_getting_drawn);
			}
		}

		public void CanvasMove(object sender, MouseEventArgs e)
		{
			if (rect_getting_drawn != null)
			{
				if (e.LeftButton == MouseButtonState.Released)
					return;

				var pos = e.GetPosition(MyCanvas);

				var x = Math.Min(pos.X, mouse_pressed_pos.X);
				var y = Math.Min(pos.Y, mouse_pressed_pos.Y);

				var w = Math.Max(pos.X, mouse_pressed_pos.X) - x;
				var h = Math.Max(pos.Y, mouse_pressed_pos.Y) - y;

				rect_getting_drawn.Width = w;
				rect_getting_drawn.Height = h;

				Canvas.SetLeft(rect_getting_drawn, x);
				Canvas.SetTop(rect_getting_drawn, y);
			}
			else if (triangle_getting_drawn != null)
			{
				if (e.LeftButton == MouseButtonState.Released)
					return;

				var pos = e.GetPosition(MyCanvas);

				Point mouse_pressed_pos_adjusted = new Point(mouse_pressed_pos.X / 1.0f, mouse_pressed_pos.Y / 1.0f);

				triangle_getting_drawn.Points[0] = new Point(mouse_pressed_pos_adjusted.X, mouse_pressed_pos_adjusted.Y);
				triangle_getting_drawn.Points[2] = pos;

				if (pos.Y < triangle_getting_drawn.Points[0].Y) {
					triangle_getting_drawn.Points[1] = new Point(triangle_getting_drawn.Points[2].X, triangle_getting_drawn.Points[0].Y);
				} else {
					triangle_getting_drawn.Points[1] = new Point(triangle_getting_drawn.Points[0].X, triangle_getting_drawn.Points[2].Y);
				}

				Canvas.SetLeft(triangle_getting_drawn, 0.0f);
				Canvas.SetTop(triangle_getting_drawn, 0.0f);
			}
			else if (line_getting_drawn != null)
			{
				if (e.LeftButton == MouseButtonState.Released)
					return;

				var pos = e.GetPosition(MyCanvas);

				Point mouse_pressed_pos_adjusted = new Point(mouse_pressed_pos.X / 1.0f, mouse_pressed_pos.Y / 1.0f);

				line_getting_drawn.X1 = mouse_pressed_pos.X;
				line_getting_drawn.Y1 = mouse_pressed_pos.Y;
				line_getting_drawn.X2 = pos.X;
				line_getting_drawn.Y2 = pos.Y;

				Canvas.SetLeft(line_getting_drawn, 0.0f);
				Canvas.SetTop(line_getting_drawn, 0.0f);
			}
		}

		public void CanvasUp(object sender, MouseButtonEventArgs e)
		{
			if (rect_getting_drawn != null)
			{
				isDataDirty = true;

				if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_RECTANGLE)
				{
					int item_index = 0;
					string name = "Rectangle " + ListOfObjects.Items.Count;

					while (GetSerializedRect(name) != null)
					{
						item_index++;
						name = "Rectangle " + item_index;
					}

					ListBoxItem item = new ListBoxItem();
					item.Content = name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);

					rectangles.Add(name, rect_getting_drawn);

					var pos = e.GetPosition(MyCanvas);

					double top_left_x = Math.Min(mouse_pressed_pos.X, pos.X);
					double top_left_y = Math.Min(mouse_pressed_pos.Y, pos.Y);

					SerializedRectangle sRect = new SerializedRectangle();
					sRect.type = "Rectangle";
					sRect.name = name;
					sRect.x = (float)Math.Round(top_left_x);
					sRect.y = (float)Math.Round(top_left_y);
					sRect.width = (float)rect_getting_drawn.Width;
					sRect.height = (float)rect_getting_drawn.Height;
					sRect.vel_x = 0.0f;
					sRect.vel_y = 0.0f;
					sRect.pass_through = false;

					sRects.Add(sRect);

					Select(name);

					rect_getting_drawn = null;
				}
				else if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_TRIGGER)
				{
					int item_index = 0;
					string name = "Trigger " + ListOfObjects.Items.Count;

					while (GetSerializedTrigger(name) != null)
					{
						item_index++;
						name = "Trigger " + item_index;
					}

					ListBoxItem item = new ListBoxItem();
					item.Content = name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);

					trigger_rects.Add(name, rect_getting_drawn);

					var pos = e.GetPosition(MyCanvas);

					double top_left_x = Math.Min(mouse_pressed_pos.X, pos.X);
					double top_left_y = Math.Min(mouse_pressed_pos.Y, pos.Y);

					SerializedTrigger sTrigger = new SerializedTrigger();
					sTrigger.type = "Trigger";
					sTrigger.name = name;
					sTrigger.x = (float)Math.Round(top_left_x);
					sTrigger.y = (float)Math.Round(top_left_y);
					sTrigger.width = (float)rect_getting_drawn.Width;
					sTrigger.height = (float)rect_getting_drawn.Height;

					sTriggers.Add(sTrigger);

					Select(name);

					rect_getting_drawn = null;
				}
			}
			else if (triangle_getting_drawn != null)
			{
				isDataDirty = true;

				if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_TRIANGLE)
				{
					int item_index = 0;
					string name = "Triangle " + ListOfObjects.Items.Count;

					while (GetSerializedTriangle(name) != null)
					{
						item_index++;
						name = "Triangle " + item_index;
					}

					ListBoxItem item = new ListBoxItem();
					item.Content = name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);

					triangles.Add(name, triangle_getting_drawn);

					SerializedTriangle sTri = new SerializedTriangle();
					sTri.type = "Triangle";
					sTri.name = name;
					sTri.right_angle_above_line = false;
					sTri.points = triangle_getting_drawn.Points;

					sTris.Add(sTri);

					Select(name);
					
					triangle_getting_drawn = null;
				}
			}
			else if (line_getting_drawn != null)
			{
				isDataDirty = true;

				if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_RAIL)
				{
					int item_index = 0;
					string name = "Rail " + ListOfObjects.Items.Count;

					while (GetSerializedRail(name) != null)
					{
						item_index++;
						name = "Rail " + item_index;
					}

					ListBoxItem item = new ListBoxItem();
					item.Content = name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);

					rail_lines.Add(name, line_getting_drawn);

					SerializedRail sRail = new SerializedRail();
					sRail.type = "Rail";
					sRail.name = name;
					sRail.x = (float)line_getting_drawn.X1;
					sRail.y = (float)line_getting_drawn.Y1;
					sRail.x_prime = (float)line_getting_drawn.X2;
					sRail.y_prime = (float)line_getting_drawn.Y2;

					sRails.Add(sRail);

					Select(name);

					line_getting_drawn = null;
				}
			}

			if (ListOfObjectTypes.SelectedIndex == OBJECT_SELECTION_DOOR)
			{
				CreateDoor(e);
			}

			if (ListOfObjectTypes.SelectedIndex >= OBJECT_SELECTION_UNIT_STARTING_INDEX)
			{
				ListBoxItem item = (ListBoxItem)ListOfObjectTypes.SelectedItem;
				string name = item.Content.ToString();

				CreateUnit(e, name);
			}
		}

		public void CreateUnit(MouseButtonEventArgs e, string bestiary_name_and_unit_type)
		{
			string bestiary_name = bestiary_name_and_unit_type.Substring(1, bestiary_name_and_unit_type.IndexOf("]") - 1);
			string unit_type = bestiary_name_and_unit_type.Substring(bestiary_name_and_unit_type.IndexOf("]") + 1);

			rect_getting_drawn = new Rectangle
			{
				Stroke = TransformingColor,
				StrokeThickness = 1,
				Width = 10.0f,
				Height = 10.0f
			};

			int item_index = 0;
			string name = unit_type + " " + ListOfObjects.Items.Count;

			while (GetSerializedUnit(name) != null)
			{
				item_index++;
				name = unit_type + " " + item_index;
			}

			ListBoxItem item = new ListBoxItem();
			item.Content = name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			ListOfObjects.Items.Add(item);

			unit_rects.Add(name, rect_getting_drawn);

			//selected_unit = null;
			//
			//for (int i = 0; i < Bestiaries.Count; i++)
			//{
			//	if (Bestiaries[i].BestiaryName == bestiary_name)
			//	{
			//		selected_unit = Bestiaries[i].DictOfUnits[unit_type];
			//		break;
			//	}
			//}

			var pos = e.GetPosition(MyCanvas);

			SerializedUnit sUnit = new SerializedUnit();
			sUnit.UnitType = unit_type;
			sUnit.BestiaryName = bestiary_name;
			sUnit.InstanceOfUnitName = name;
			sUnit.LevelLocationX = (float)pos.X;
			sUnit.LevelLocationY = (float)pos.Y;
			sUnit.width = (float)rect_getting_drawn.Width;
			sUnit.height = (float)rect_getting_drawn.Height;
			sUnit.IsInteractable = false;
			sUnit.IsDestructible = false;

			sUnits.Add(sUnit);

			Select(name);
			
			Canvas.SetLeft(rect_getting_drawn, sUnit.LevelLocationX);
			Canvas.SetTop(rect_getting_drawn, sUnit.LevelLocationY);
			MyCanvas.Children.Add(rect_getting_drawn);

			rect_getting_drawn = null;
		}

		public void CreateDoor(MouseButtonEventArgs e)
		{
			rect_getting_drawn = new Rectangle
			{
				Stroke = TransformingColor,
				StrokeThickness = 1,
				Width = 4.0f,
				Height = 20.0f
			};

			int item_index = 0;
			string name = "Door " + ListOfObjects.Items.Count;

			while (GetSerializedDoor(name) != null)
			{
				item_index++;
				name = "Door " + item_index;
			}

			ListBoxItem item = new ListBoxItem();
			item.Content = name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			ListOfObjects.Items.Add(item);

			door_rects.Add(name, rect_getting_drawn);

			var pos = e.GetPosition(MyCanvas);

			SerializedDoor sDoor = new SerializedDoor();
			sDoor.type = "Door";
			sDoor.name = name;
			sDoor.x = (float)pos.X;
			sDoor.y = (float)pos.Y;
			sDoor.width = (float)rect_getting_drawn.Width;
			sDoor.height = (float)rect_getting_drawn.Height;
			sDoor.activator = "Player 0";

			sDoors.Add(sDoor);

			Select(name);
			
			Canvas.SetLeft(rect_getting_drawn, sDoor.x);
			Canvas.SetTop(rect_getting_drawn, sDoor.y);
			MyCanvas.Children.Add(rect_getting_drawn);

			rect_getting_drawn = null;
		}

		public void CreatePlayer()
		{
			rect_getting_drawn = new Rectangle
			{
				Stroke = TransformingColor,
				StrokeThickness = 2,
				Width = 6.0f,
				Height = 12.0f
			};

			string name = "Player 0";

			sPlayer = new SerializedPlayer();
			sPlayer.type = "Player";
			sPlayer.name = name;
			sPlayer.x = 800.0f;
			sPlayer.y = 500.0f;
			sPlayer.width = (float)rect_getting_drawn.Width;
			sPlayer.height = (float)rect_getting_drawn.Height;

			Canvas.SetLeft(rect_getting_drawn, sPlayer.x);
			Canvas.SetTop(rect_getting_drawn, sPlayer.y);
			MyCanvas.Children.Add(rect_getting_drawn);

			ListBoxItem item = new ListBoxItem();
			item.Content = name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			ListOfObjects.Items.Add(item);

			playerRectangle = rect_getting_drawn;

			Select(name);

			rect_getting_drawn = null;
		}

		private void OnSelected(object sender, RoutedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			string name = item.Content.ToString();

			Deselect();
			Select(name);
		}

		private void OnUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void Select(string name)
		{
			if (name.Contains("Rectangle"))
			{
				DeleteButton.IsEnabled = true;

				selected_rect_name = name;

				selected_rectangle = rectangles[name];
				selected_rectangle.Stroke = SelectedColor;

				SerializedRectangle sRect = GetSerializedRect(name);

				xTextBox.Text = sRect.x.ToString();
				yTextBox.Text = sRect.y.ToString();
				wTextBox.Text = sRect.width.ToString();
				hTextBox.Text = sRect.height.ToString();
				wTextBox.IsEnabled = true;
				hTextBox.IsEnabled = true;

				velocityXTextBox.Text = sRect.vel_x.ToString();
				velocityYTextBox.Text = sRect.vel_y.ToString();
				TriangleCheckBox.IsEnabled = false;
				PassThroughCheckBox.IsEnabled = true;
				PassThroughCheckBox.IsChecked = sRect.pass_through;

				HideTriangleUiItems();
				HideUnitUiItems();
				HideDoorUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowRectangleUiItems();
			}
			else if (name.Contains("Triangle"))
			{
				DeleteButton.IsEnabled = true;

				selected_triangle_name = name;

				selected_triangle = triangles[name];
				selected_triangle.Stroke = SelectedColor;

				SerializedTriangle sTri = GetSerializedTriangle(name);

				p1xTextBox.Text = sTri.points[0].X.ToString();
				p1yTextBox.Text = sTri.points[0].Y.ToString();
				p2xTextBox.Text = sTri.points[2].X.ToString();
				p2yTextBox.Text = sTri.points[2].Y.ToString();

				TriangleCheckBox.IsEnabled = true;
				TriangleCheckBox.IsChecked = sTri.right_angle_above_line;

				HideRectangleUiItems();
				HideDoorUiItems();
				HideUnitUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowTriangleUiItems();
			}
			else if (name.Contains("Rail"))
			{
				DeleteButton.IsEnabled = true;

				selected_rail_name = name;

				selected_rail = rail_lines[name];
				selected_rail.Stroke = SelectedColor;

				SerializedRail sRail = GetSerializedRail(name);

				p1xTextBox.Text = sRail.x.ToString();
				p1yTextBox.Text = sRail.y.ToString();
				p2xTextBox.Text = sRail.x_prime.ToString();
				p2yTextBox.Text = sRail.y_prime.ToString();

				HideRectangleUiItems();
				HideDoorUiItems();
				HideUnitUiItems();
				HideTriggerUiItems();
				HideTriangleUiItems();
				ShowRailUiItems();
			}
			else if (name.Contains("Player"))
			{
				DeleteButton.IsEnabled = false;

				playerRectSelected = true;

				playerRectangle.Stroke = SelectedColor;

				xTextBox.Text = sPlayer.x.ToString();
				yTextBox.Text = sPlayer.y.ToString();
				wTextBox.Text = sPlayer.width.ToString();
				hTextBox.Text = sPlayer.height.ToString();
				wTextBox.IsEnabled = true;
				hTextBox.IsEnabled = true;
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideDoorUiItems();
				HideUnitUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowRectangleUiItems();
			}
			else if (name.Contains("Door"))
			{
				DeleteButton.IsEnabled = true;

				selected_door_name = name;

				selected_door = door_rects[name];
				selected_door.Stroke = SelectedColor;

				SerializedDoor sDoor = GetSerializedDoor(name);

				xTextBox.Text = sDoor.x.ToString();
				yTextBox.Text = sDoor.y.ToString();
				wTextBox.Text = sDoor.width.ToString();
				hTextBox.Text = sDoor.height.ToString();
				doorActivatorTextBox.Text = sDoor.activator;

				wTextBox.IsEnabled = false;
				hTextBox.IsEnabled = false;
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideRectangleUiItems();
				HideUnitUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowDoorUiItems();
			}
			else if (name.Contains("Trigger"))
			{
				DeleteButton.IsEnabled = true;

				selected_trigger_name = name;

				selected_trigger = trigger_rects[name];
				selected_trigger.Stroke = SelectedColor;

				SerializedTrigger sTrigger = GetSerializedTrigger(name);

				xTextBox.Text = sTrigger.x.ToString();
				yTextBox.Text = sTrigger.y.ToString();
				wTextBox.Text = sTrigger.width.ToString();
				hTextBox.Text = sTrigger.height.ToString();
				activityTextBox.Text = sTrigger.activity;

				wTextBox.IsEnabled = false;
				hTextBox.IsEnabled = false;
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideUnitUiItems();
				HideDoorUiItems();
				HideRectangleUiItems();
				HideRailUiItems();
				ShowTriggerUiItems();
			}
			else if (name.Contains("Image"))
			{
				DeleteButton.IsEnabled = true;

				selected_image_name = name;

				ArtImage image = GetArtImage(name);
				selected_image = image;

				xTextBox.Text = image.x.ToString();
				yTextBox.Text = image.y.ToString();
				wTextBox.Text = "";
				hTextBox.Text = "";
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideUnitUiItems();
				HideDoorUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowRectangleUiItems();
			}
			else if (name.Contains("Layer"))
			{
				DeleteButton.IsEnabled = true;

				Zone zone = GetCurrentSelectedZone();
				if (zone != null)
				{
					xZoneTextBox.Text = zone.x.ToString();
					yZoneTextBox.Text = zone.y.ToString();
					TriangleCheckBox.IsEnabled = false;

					HideTriangleUiItems();
					HideUnitUiItems();
					HideDoorUiItems();
					HideTriggerUiItems();
					HideRailUiItems();
					ShowRectangleUiItems();
				}
			}
			else
			{
				DeleteButton.IsEnabled = true;

				selected_unit_name = name;

				selected_unit_rectangle = unit_rects[name];
				selected_unit_rectangle.Stroke = SelectedColor;

				SerializedUnit unit = GetSerializedUnit(name);

				xTextBox.Text = unit.LevelLocationX.ToString();
				yTextBox.Text = unit.LevelLocationY.ToString();
				wTextBox.Text = "N/A";
				hTextBox.Text = "N/A";
				activityTextBox.Text = unit.activity;

				wTextBox.IsEnabled = false;
				hTextBox.IsEnabled = false;
				TriangleCheckBox.IsEnabled = false;

				IsInteractableCheckBox.IsEnabled = true;
				IsInteractableCheckBox.IsChecked = unit.IsInteractable;

				IsDestructibleCheckBox.IsEnabled = true;
				IsDestructibleCheckBox.IsChecked = unit.IsDestructible;

				HideTriangleUiItems();
				HideDoorUiItems();
				HideRectangleUiItems();
				HideTriggerUiItems();
				HideRailUiItems();
				ShowUnitUiItems();
			}
		}

		private void Deselect()
		{
			if (playerRectSelected)
			{
				playerRectangle.Stroke = DormantColor;
				HideRectangleUiItems();
			}
			else if (selected_rectangle != null)
			{
				selected_rectangle.Stroke = DormantColor;
				HideRectangleUiItems();
			}
			else if (selected_triangle != null)
			{
				selected_triangle.Stroke = DormantColor;
				HideTriangleUiItems();
			}
			else if (selected_door != null)
			{
				selected_door.Stroke = DormantColor;
				HideDoorUiItems();
			}
			else if (selected_unit_rectangle != null)
			{
				selected_unit_rectangle.Stroke = DormantColor;
				HideRectangleUiItems();
			}
			else if (selected_trigger != null)
			{
				selected_trigger.Stroke = DormantColor;
				HideRectangleUiItems();
			}
			else if (selected_image != null)
			{
				HideRectangleUiItems();
			}
			else if (selected_rail != null)
			{
				selected_rail.Stroke = DormantColor;
				HideRailUiItems();
			}

			DeleteButton.IsEnabled = false;

			playerRectSelected = false;

			selected_rect_name = "";
			selected_rectangle = null;

			selected_triangle_name = "";
			selected_triangle = null;

			selected_rail_name = "";
			selected_rail = null;

			selected_door_name = "";
			selected_door = null;

			//selected_unit = null;
			selected_unit_name = "";
			selected_unit_rectangle = null;

			selected_trigger_name = "";
			selected_trigger = null;

			selected_image_name = "";
			selected_image = null;

			//selected_parallaxing_background_name = "";
			//selected_parallaxing_background = null;

			xTextBox.Text = "";
			yTextBox.Text = "";
			wTextBox.Text = "";
			hTextBox.Text = "";
			velocityXTextBox.Text = "";
			velocityYTextBox.Text = "";
			TriangleCheckBox.IsEnabled = false;

			activityTextBox.Text = "";
			doorActivatorTextBox.Text = "";
		}

		private void Delete(object sender, RoutedEventArgs e)
		{
			SerializedRectangle sRect = GetSerializedRect(selected_rect_name);
			if (sRect != null)
			{
				isDataDirty = true;
				rectangles.Remove(selected_rect_name);
				sRects.Remove(sRect);
				MyCanvas.Children.Remove(selected_rectangle);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_rect_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			SerializedTriangle sTri = GetSerializedTriangle(selected_triangle_name);
			if (sTri != null)
			{
				isDataDirty = true;
				triangles.Remove(selected_triangle_name);
				sTris.Remove(sTri);
				MyCanvas.Children.Remove(selected_triangle);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_triangle_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			SerializedRail sRail = GetSerializedRail(selected_rail_name);
			if (sRail != null)
			{
				isDataDirty = true;
				rail_lines.Remove(selected_rail_name);
				sRails.Remove(sRail);
				MyCanvas.Children.Remove(selected_rail);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_rail_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			SerializedDoor sDoor = GetSerializedDoor(selected_door_name);
			if (sDoor != null)
			{
				isDataDirty = true;
				door_rects.Remove(selected_door_name);
				sDoors.Remove(sDoor);
				MyCanvas.Children.Remove(selected_door);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_door_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			SerializedUnit sUnit = GetSerializedUnit(selected_unit_name);
			if (sUnit != null)
			{
				isDataDirty = true;
				unit_rects.Remove(selected_unit_name);
				sUnits.Remove(sUnit);
				MyCanvas.Children.Remove(selected_unit_rectangle);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_unit_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			SerializedTrigger sTrigger = GetSerializedTrigger(selected_trigger_name);
			if (sTrigger != null)
			{
				isDataDirty = true;
				trigger_rects.Remove(selected_trigger_name);
				sTriggers.Remove(sTrigger);
				MyCanvas.Children.Remove(selected_trigger);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_trigger_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			ArtImage image = GetArtImage(selected_image_name);
			if (image != null)
			{
				isDataDirty = true;
				artImages.Remove(image);
				MyCanvas.Children.Remove(image.image);

				for (int i = 0; i < ListOfObjects.Items.Count; i++)
				{
					ListBoxItem item = (ListBoxItem)ListOfObjects.Items[i];

					if (item.Content.ToString() == selected_image_name)
					{
						ListOfObjects.Items.Remove(ListOfObjects.Items[i]);
						break;
					}
				}
			}

			Zone zone = GetCurrentSelectedZone();
			if (zone != null)
			{
				ParallaxingBackground parallaxing_background = zone.GetCurrentlySelectedParallaxingBackground();
				if (parallaxing_background != null)
				{
					isDataDirty = true;
					zone.ParallaxingBackgrounds.Remove(parallaxing_background);
					MyCanvas.Children.Remove(parallaxing_background.image);

					for (int i = 0; i < zone.ZonesListOfParallaxingBackgrounds.Count; i++)
					{
						ListBoxItem item = (ListBoxItem)zone.ZonesListOfParallaxingBackgrounds[i];

						if (item.Content.ToString() == parallaxing_background.name)
						{
							zone.ZonesListOfParallaxingBackgrounds.Remove(zone.ZonesListOfParallaxingBackgrounds[i]);
							ListOfParallaxingBackgrounds.Items.Remove(zone.ZonesListOfParallaxingBackgrounds[i]);
							break;
						}
					}
				}
			}
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			SerializedObjects sObjects = new SerializedObjects();
			sObjects.player = sPlayer;
			sObjects.rectangles = sRects;
			sObjects.triangles = sTris;
			sObjects.rails = sRails;
			sObjects.doors = sDoors;
			sObjects.triggers = sTriggers;
			sObjects.bestiaryFilePaths = ImportedBestiariesFilePaths;
			sObjects.units = sUnits;
			sObjects.images = artImages;
			sObjects.zones = zones;
			//sObjects.parallaxingBackgrounds = parallaxingBackgrounds;

			string jsonStr = JsonConvert.SerializeObject(sObjects);

			Stream myStream;
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.InitialDirectory = Settings.root_directory;
			saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			saveFileDialog.FilterIndex = 2;

			if (saveFileDialog.ShowDialog() == true)
			{
				if ((myStream = saveFileDialog.OpenFile()) != null)
				{
					StreamWriter writer = new StreamWriter(myStream);

					writer.Write(jsonStr);
					writer.Flush();
					myStream.Position = 0;

					myStream.Close();

					isDataDirty = false;
				}
			}
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			string result = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.root_directory;

			openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog.FileName);

				result = sr.ReadToEnd();

				sr.Close();
			}

			if (!string.IsNullOrWhiteSpace(result))
			{
				SerializedObjects sObjects = JsonConvert.DeserializeObject<SerializedObjects>(result);

				sPlayer = sObjects.player;
				sRects = sObjects.rectangles;
				sTris = sObjects.triangles;
				sRails = sObjects.rails ?? new List<SerializedRail>();
				sDoors = sObjects.doors;
				sTriggers = sObjects.triggers;
				ImportedBestiariesFilePaths = sObjects.bestiaryFilePaths;
				sUnits = sObjects.units;
				artImages = sObjects.images;
				zones = sObjects.zones;
				//parallaxingBackgrounds = sObjects.parallaxingBackgrounds;

				rectangles.Clear();
				triangles.Clear();
				rail_lines.Clear();
				door_rects.Clear();
				unit_rects.Clear();
				trigger_rects.Clear();

				ListOfObjectTypes.Items.Clear();

				ListBoxItem rectangleItem = new ListBoxItem();
				rectangleItem.Content = "Rectangle";
				ListOfObjectTypes.Items.Add(rectangleItem);

				ListBoxItem triangleItem = new ListBoxItem();
				triangleItem.Content = "Triangle";
				ListOfObjectTypes.Items.Add(triangleItem);

				ListBoxItem doorItem = new ListBoxItem();
				doorItem.Content = "Door";
				ListOfObjectTypes.Items.Add(doorItem);

				ListBoxItem triggerItem = new ListBoxItem();
				triggerItem.Content = "Trigger";
				ListOfObjectTypes.Items.Add(triggerItem);

				ListBoxItem railItem = new ListBoxItem();
				railItem.Content = "Rail";
				ListOfObjectTypes.Items.Add(railItem);

				ListOfObjects.Items.Clear();
				MyCanvas.Children.Clear();

				Rectangle rect = new Rectangle
				{
					Stroke = DormantColor,
					StrokeThickness = 2,
					Height = sPlayer.height,
					Width = sPlayer.width
				};
				Canvas.SetLeft(rect, sPlayer.x);
				Canvas.SetTop(rect, sPlayer.y);
				MyCanvas.Children.Add(rect);

				playerRectangle = rect;

				ListBoxItem item = new ListBoxItem();
				item.Content = sPlayer.name;
				item.Selected += OnSelected;
				item.Unselected += OnUnselected;
				ListOfObjects.Items.Add(item);

				for (int i = 0; i < sRects.Count(); i++)
				{
					rect = new Rectangle
					{
						Stroke = DormantColor,
						StrokeThickness = 1,
						Height = sRects[i].height,
						Width = sRects[i].width
					};
					Canvas.SetLeft(rect, sRects[i].x);
					Canvas.SetTop(rect, sRects[i].y);
					MyCanvas.Children.Add(rect);

					rectangles.Add(sRects[i].name, rect);

					item = new ListBoxItem();
					item.Content = sRects[i].name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}

				for (int i = 0; i < sTris.Count(); i++)
				{
					Polygon triangle = new Polygon();
					triangle.Stroke = DormantColor;
					triangle.StrokeThickness = 1;
					triangle.HorizontalAlignment = HorizontalAlignment.Left;
					triangle.VerticalAlignment = VerticalAlignment.Top;

					triangle.Points = sTris[i].points;

					Canvas.SetLeft(triangle, 0.0f);
					Canvas.SetTop(triangle, 0.0f);
					MyCanvas.Children.Add(triangle);

					triangles.Add(sTris[i].name, triangle);

					item = new ListBoxItem();
					item.Content = sTris[i].name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}
				
				for (int i = 0; i < sRails.Count(); i++)
				{
					Line rail = new Line();
					rail.Stroke = DormantColor;
					rail.StrokeThickness = 1;
					rail.HorizontalAlignment = HorizontalAlignment.Left;
					rail.VerticalAlignment = VerticalAlignment.Top;

					rail.X1 = sRails[i].x;
					rail.Y1 = sRails[i].y;
					rail.X2 = sRails[i].x_prime;
					rail.Y2 = sRails[i].y_prime;

					Canvas.SetLeft(rail, 0.0f);
					Canvas.SetTop(rail, 0.0f);
					MyCanvas.Children.Add(rail);

					rail_lines.Add(sRails[i].name, rail);

					item = new ListBoxItem();
					item.Content = sRails[i].name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}

				for (int i = 0; i < sDoors.Count(); i++)
				{
					rect = new Rectangle
					{
						Stroke = DormantColor,
						StrokeThickness = 1,
						Height = sDoors[i].height,
						Width = sDoors[i].width
					};
					Canvas.SetLeft(rect, sDoors[i].x);
					Canvas.SetTop(rect, sDoors[i].y);
					MyCanvas.Children.Add(rect);

					door_rects.Add(sDoors[i].name, rect);

					item = new ListBoxItem();
					item.Content = sDoors[i].name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}

				for (int i = 0; i < sUnits.Count(); i++)
				{
					rect = new Rectangle
					{
						Stroke = DormantColor,
						StrokeThickness = 1,
						Height = sUnits[i].height,
						Width = sUnits[i].width
					};
					Canvas.SetLeft(rect, sUnits[i].LevelLocationX);
					Canvas.SetTop(rect, sUnits[i].LevelLocationY);
					MyCanvas.Children.Add(rect);

					unit_rects.Add(sUnits[i].InstanceOfUnitName, rect);

					item = new ListBoxItem();
					item.Content = sUnits[i].InstanceOfUnitName;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}

				for (int i = 0; i < sTriggers.Count(); i++)
				{
					rect = new Rectangle
					{
						Stroke = DormantColor,
						StrokeThickness = 1,
						Height = sTriggers[i].height,
						Width = sTriggers[i].width
					};
					Canvas.SetLeft(rect, sTriggers[i].x);
					Canvas.SetTop(rect, sTriggers[i].y);
					MyCanvas.Children.Add(rect);

					trigger_rects.Add(sTriggers[i].name, rect);

					item = new ListBoxItem();
					item.Content = sTriggers[i].name;
					item.Selected += OnSelected;
					item.Unselected += OnUnselected;
					ListOfObjects.Items.Add(item);
				}

				for (int i = 0; i < artImages.Count(); i++)
				{
					LoadArtImage(artImages[i]);
				}

				if (zones != null)
				{
					for (int zone_index = 0; zone_index < zones.Count(); zone_index++)
					{
						if (zone_index == 0)
						{
							xZoneTextBox.Text = zones[0].x.ToString();
							yZoneTextBox.Text = zones[0].y.ToString();
						}

						for (int i = 0; i < zones[zone_index].ParallaxingBackgrounds.Count(); i++)
						{
							LoadParallaxingBackground(zones[zone_index].ParallaxingBackgrounds[i], zones[zone_index]);
						}
					}
				}

				ResetParallaxingBackgroundsComboxBox();

				ImportBestiaries();

				isDataDirty = false;
				Deselect();
			}
		}

		private ArtImage GetArtImage(string name)
		{
			for (int i = 0; i < artImages.Count; i++)
			{
				if (name == artImages[i].name)
				{
					return artImages[i];
				}
			}

			return null;
		}

		//private ParallaxingBackground GetParallaxingBackground(string name)
		//{
		//	Zone zone = GetCurrentSelectedZone();
		//	if (zone != null)
		//	{
		//		for (int i = 0; i < zone.ParallaxingBackgrounds.Count; i++)
		//		{
		//			if (name == zone.ParallaxingBackgrounds[i].name)
		//			{
		//				return zone.ParallaxingBackgrounds[i];
		//			}
		//		}
		//	}
		//
		//	return null;
		//}

		private SerializedRectangle GetSerializedRect(string name)
		{
			for (int i = 0; i < sRects.Count; i++)
			{
				if (name == sRects[i].name)
				{
					return sRects[i];
				}
			}

			return null;
		}

		private SerializedTriangle GetSerializedTriangle(string name)
		{
			for (int i = 0; i < sTris.Count; i++)
			{
				if (name == sTris[i].name)
				{
					return sTris[i];
				}
			}

			return null;
		}

		private SerializedDoor GetSerializedDoor(string name)
		{
			for (int i = 0; i < sDoors.Count; i++)
			{
				if (name == sDoors[i].name)
				{
					return sDoors[i];
				}
			}

			return null;
		}

		private SerializedTrigger GetSerializedTrigger(string name)
		{
			for (int i = 0; i < sTriggers.Count; i++)
			{
				if (name == sTriggers[i].name)
				{
					return sTriggers[i];
				}
			}

			return null;
		}

		private SerializedRail GetSerializedRail(string name)
		{
			for (int i = 0; i < sRails.Count; i++)
			{
				if (name == sRails[i].name)
				{
					return sRails[i];
				}
			}

			return null;
		}

		private SerializedUnit GetSerializedUnit(string name)
		{
			for (int i = 0; i < sUnits.Count; i++)
			{
				if (name == sUnits[i].InstanceOfUnitName)
				{
					return sUnits[i];
				}
			}

			return null;
		}

		void DataWindow_Closing(object sender, CancelEventArgs e)
		{
			// If data is dirty, notify user and ask for a response
			if (isDataDirty)
			{
				string msg = "Data is dirty. Close without saving?";
				MessageBoxResult result =
				  MessageBox.Show(
					msg,
					"Data App",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);
				if (result == MessageBoxResult.No)
				{
					// If user doesn't want to close, cancel closure
					e.Cancel = true;
				}
			}
		}

		void ShowRectangleUiItems()
		{
			xTextBox.Visibility = Visibility.Visible;
			yTextBox.Visibility = Visibility.Visible;
			wTextBox.Visibility = Visibility.Visible;
			hTextBox.Visibility = Visibility.Visible;
			velocityXTextBox.Visibility = Visibility.Visible;
			velocityYTextBox.Visibility = Visibility.Visible;
			

			xLabel.Visibility = Visibility.Visible;
			yLabel.Visibility = Visibility.Visible;
			wLabel.Visibility = Visibility.Visible;
			hLabel.Visibility = Visibility.Visible;
			velocityXLabel.Visibility = Visibility.Visible;
			velocityYLabel.Visibility = Visibility.Visible;
			PassThroughCheckBox.Visibility = Visibility.Visible;
		}

		void HideRectangleUiItems()
		{
			xTextBox.Visibility = Visibility.Hidden;
			yTextBox.Visibility = Visibility.Hidden;
			wTextBox.Visibility = Visibility.Hidden;
			hTextBox.Visibility = Visibility.Hidden;
			velocityXTextBox.Visibility = Visibility.Hidden;
			velocityYTextBox.Visibility = Visibility.Hidden;

			xLabel.Visibility = Visibility.Hidden;
			yLabel.Visibility = Visibility.Hidden;
			wLabel.Visibility = Visibility.Hidden;
			hLabel.Visibility = Visibility.Hidden;
			velocityXLabel.Visibility = Visibility.Hidden;
			velocityYLabel.Visibility = Visibility.Hidden;
			PassThroughCheckBox.Visibility = Visibility.Hidden;
		}

		void ShowTriggerUiItems()
		{
			xTextBox.Visibility = Visibility.Visible;
			yTextBox.Visibility = Visibility.Visible;
			wTextBox.Visibility = Visibility.Visible;
			hTextBox.Visibility = Visibility.Visible;

			xLabel.Visibility = Visibility.Visible;
			yLabel.Visibility = Visibility.Visible;
			wLabel.Visibility = Visibility.Visible;
			hLabel.Visibility = Visibility.Visible;

			activityTextBox.Visibility = Visibility.Visible;
			activityLabel.Visibility = Visibility.Visible;
		}

		void HideTriggerUiItems()
		{
			xTextBox.Visibility = Visibility.Hidden;
			yTextBox.Visibility = Visibility.Hidden;
			wTextBox.Visibility = Visibility.Hidden;
			hTextBox.Visibility = Visibility.Hidden;

			xLabel.Visibility = Visibility.Hidden;
			yLabel.Visibility = Visibility.Hidden;
			wLabel.Visibility = Visibility.Hidden;
			hLabel.Visibility = Visibility.Hidden;

			activityTextBox.Visibility = Visibility.Hidden;
			activityLabel.Visibility = Visibility.Hidden;
		}

		void ShowUnitUiItems()
		{
			xTextBox.Visibility = Visibility.Visible;
			yTextBox.Visibility = Visibility.Visible;
			wTextBox.Visibility = Visibility.Visible;
			hTextBox.Visibility = Visibility.Visible;

			xLabel.Visibility = Visibility.Visible;
			yLabel.Visibility = Visibility.Visible;
			wLabel.Visibility = Visibility.Visible;
			hLabel.Visibility = Visibility.Visible;

			activityTextBox.Visibility = Visibility.Visible;
			activityLabel.Visibility = Visibility.Visible;

			IsInteractableCheckBox.Visibility = Visibility.Visible;
			IsDestructibleCheckBox.Visibility = Visibility.Visible;
		}

		void HideUnitUiItems()
		{
			xTextBox.Visibility = Visibility.Hidden;
			yTextBox.Visibility = Visibility.Hidden;
			wTextBox.Visibility = Visibility.Hidden;
			hTextBox.Visibility = Visibility.Hidden;

			xLabel.Visibility = Visibility.Hidden;
			yLabel.Visibility = Visibility.Hidden;
			wLabel.Visibility = Visibility.Hidden;
			hLabel.Visibility = Visibility.Hidden;

			activityTextBox.Visibility = Visibility.Hidden;
			activityLabel.Visibility = Visibility.Hidden;

			IsInteractableCheckBox.Visibility = Visibility.Hidden;
			IsDestructibleCheckBox.Visibility = Visibility.Hidden;
		}

		void ShowTriangleUiItems()
		{
			p1xTextBox.Visibility = Visibility.Visible;
			p1yTextBox.Visibility = Visibility.Visible;
			p2xTextBox.Visibility = Visibility.Visible;
			p2yTextBox.Visibility = Visibility.Visible;

			p1xLabel.Visibility = Visibility.Visible;
			p1yLabel.Visibility = Visibility.Visible;
			p2xLabel.Visibility = Visibility.Visible;
			p2yLabel.Visibility = Visibility.Visible;

			TriangleCheckBox.Visibility = Visibility.Visible;
		}

		void HideTriangleUiItems()
		{
			p1xTextBox.Visibility = Visibility.Hidden;
			p1yTextBox.Visibility = Visibility.Hidden;
			p2xTextBox.Visibility = Visibility.Hidden;
			p2yTextBox.Visibility = Visibility.Hidden;

			p1xLabel.Visibility = Visibility.Hidden;
			p1yLabel.Visibility = Visibility.Hidden;
			p2xLabel.Visibility = Visibility.Hidden;
			p2yLabel.Visibility = Visibility.Hidden;

			TriangleCheckBox.Visibility = Visibility.Hidden;
		}

		void ShowRailUiItems()
		{
			p1xTextBox.Visibility = Visibility.Visible;
			p1yTextBox.Visibility = Visibility.Visible;
			p2xTextBox.Visibility = Visibility.Visible;
			p2yTextBox.Visibility = Visibility.Visible;

			p1xLabel.Visibility = Visibility.Visible;
			p1yLabel.Visibility = Visibility.Visible;
			p2xLabel.Visibility = Visibility.Visible;
			p2yLabel.Visibility = Visibility.Visible;
		}

		void HideRailUiItems()
		{
			p1xTextBox.Visibility = Visibility.Hidden;
			p1yTextBox.Visibility = Visibility.Hidden;
			p2xTextBox.Visibility = Visibility.Hidden;
			p2yTextBox.Visibility = Visibility.Hidden;

			p1xLabel.Visibility = Visibility.Hidden;
			p1yLabel.Visibility = Visibility.Hidden;
			p2xLabel.Visibility = Visibility.Hidden;
			p2yLabel.Visibility = Visibility.Hidden;
		}

		void ShowDoorUiItems()
		{
			xTextBox.Visibility = Visibility.Visible;
			yTextBox.Visibility = Visibility.Visible;
			wTextBox.Visibility = Visibility.Visible;
			hTextBox.Visibility = Visibility.Visible;

			xLabel.Visibility = Visibility.Visible;
			yLabel.Visibility = Visibility.Visible;
			wLabel.Visibility = Visibility.Visible;
			hLabel.Visibility = Visibility.Visible;

			doorActivatorLabel.Visibility = Visibility.Visible;
			doorActivatorTextBox.Visibility = Visibility.Visible;
		}

		void HideDoorUiItems()
		{
			xTextBox.Visibility = Visibility.Hidden;
			yTextBox.Visibility = Visibility.Hidden;
			wTextBox.Visibility = Visibility.Hidden;
			hTextBox.Visibility = Visibility.Hidden;

			xLabel.Visibility = Visibility.Hidden;
			yLabel.Visibility = Visibility.Hidden;
			wLabel.Visibility = Visibility.Hidden;
			hLabel.Visibility = Visibility.Hidden;

			doorActivatorLabel.Visibility = Visibility.Hidden;
			doorActivatorTextBox.Visibility = Visibility.Hidden;
		}

		private void ImportBestiaryButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.root_directory;

			openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				ImportBestiary(openFileDialog.FileName);
			}
		}

		private void ImportBestiaries()
		{
			for (int i = 0; i < ImportedBestiariesFilePaths.Count; i++)
			{
				ImportBestiary(ImportedBestiariesFilePaths[i]);
			}
		}

		private void ImportBestiary(string filepath)
		{
			string result = "";

			if (filepath.Contains(Settings.root_directory))
			{
				filepath = filepath.Remove(0, Settings.root_directory.Length);
			}

			System.IO.StreamReader sr = new System.IO.StreamReader(Settings.root_directory + filepath);

			result = sr.ReadToEnd();

			sr.Close();

			if (!string.IsNullOrWhiteSpace(result))
			{
				Bestiary newBestiary = JsonConvert.DeserializeObject<Bestiary>(result);

				foreach (string key in newBestiary.DictOfUnits.Keys)
				{
					Unit unit = newBestiary.DictOfUnits[key];

					ListBoxItem item = new ListBoxItem();
					item.Content = "[" + newBestiary.BestiaryName + "]" + unit.UnitName;
					ListOfObjectTypes.Items.Add(item);
				}

				Bestiaries.Add(newBestiary);

				string rootDirRelativeFilePathForSaving = filepath;
				if (filepath.Contains(Settings.root_directory))
				{
					rootDirRelativeFilePathForSaving = filepath.Remove(0, Settings.root_directory.Length);
				}

				if (!ImportedBestiariesFilePaths.Contains(rootDirRelativeFilePathForSaving))
				{
					ImportedBestiariesFilePaths.Add(rootDirRelativeFilePathForSaving);
				}

				isDataDirty = false;
			}
		}

		private void AddImageToObject_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.root_directory;

			openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
				{
					CreateArtImage(openFileDialog.FileName, true);
				}
			}
		}

		private void AddImage_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.root_directory;

			openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
				{
					CreateArtImage(openFileDialog.FileName, false);
				}
			}
		}

		public void CreateArtImage(string file_path_and_name, bool tie_to_selected_object)
		{
			if (tie_to_selected_object && (playerRectSelected || selected_triangle_name != "" || selected_rail_name != "" || selected_unit_name != "" || selected_trigger_name != ""))
			{
				return;
			}

			BitmapImage bitmap = new BitmapImage(new Uri(file_path_and_name, UriKind.Absolute));
			Image image = new Image();
			image.Source = bitmap;
			image.Height = bitmap.PixelHeight / 6.0;
			image.Width = bitmap.PixelWidth / 6.0;

			ArtImage newArtImage = new ArtImage();

			if (file_path_and_name.Contains(Settings.root_directory))
			{
				file_path_and_name = file_path_and_name.Remove(0, Settings.root_directory.Length);
			}
			newArtImage.FilePath = file_path_and_name;

			float left = 0.0f;
			float top = 0.0f;

			if (tie_to_selected_object)
			{
				SerializedRectangle sRect = GetSerializedRect(selected_rect_name);
				if (sRect != null)
				{
					left = sRect.x + (sRect.width / 2.0f) - ((float)image.Width / 2.0f);
					top = sRect.y + (sRect.height / 2.0f) - ((float)image.Height / 2.0f);
					sRect.TiedArtImageFileName = newArtImage.FilePath;
				}

				SerializedDoor sDoor = GetSerializedDoor(selected_door_name);
				if (sDoor != null)
				{
					left = sDoor.x + (sDoor.width / 2.0f) - ((float)image.Width / 2.0f);
					top = sDoor.y + (sDoor.height / 2.0f) - ((float)image.Height / 2.0f);
					sDoor.TiedArtImageFileName = newArtImage.FilePath;
				}
			}

			newArtImage.x = left;
			newArtImage.y = top;
			newArtImage.PixelWidth = bitmap.PixelWidth;
			newArtImage.PixelHeight = bitmap.PixelHeight;
			newArtImage.image = image;
			newArtImage.IsStandalone = !tie_to_selected_object;
			Canvas.SetLeft(image, newArtImage.x);
			Canvas.SetTop(image, newArtImage.y);

			int item_index = 0;
			string name = "Image " + ListOfObjects.Items.Count;

			while (GetArtImage(name) != null)
			{
				item_index++;
				name = "Image " + item_index;
			}

			newArtImage.name = name;

			ListBoxItem item = new ListBoxItem();
			item.Content = name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			ListOfObjects.Items.Add(item);

			artImages.Add(newArtImage);
			MyCanvas.Children.Add(image);
		}

		public void LoadArtImage(ArtImage art_image)
		{
			BitmapImage bitmap = new BitmapImage(new Uri(Settings.root_directory + art_image.FilePath, UriKind.Absolute));

			Image image = new Image();
			image.Source = bitmap;
			image.Height = bitmap.PixelHeight / 6.0;
			image.Width = bitmap.PixelWidth / 6.0;

			art_image.PixelWidth = bitmap.PixelWidth;
			art_image.PixelHeight = bitmap.PixelHeight;
			art_image.image = image;

			Canvas.SetLeft(image, art_image.x);
			Canvas.SetTop(image, art_image.y);

			MyCanvas.Children.Add(image);

			if (string.IsNullOrWhiteSpace(art_image.name))
			{
				int item_index = 0;
				string name = "Image " + ListOfObjects.Items.Count;

				while (GetArtImage(name) != null)
				{
					item_index++;
					name = "Image " + item_index;
				}

				art_image.name = name;
			}

			ListBoxItem item = new ListBoxItem();
			item.Content = art_image.name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			ListOfObjects.Items.Add(item);
		}

		private void AddParallaxingBackground_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Settings.root_directory;

			openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				if (!string.IsNullOrWhiteSpace(openFileDialog.FileName))
				{
					CreateParallaxingBackground(openFileDialog.FileName);
				}
			}
		}

		public void CreateParallaxingBackground(string file_path_and_name)
		{
			Zone zone = GetCurrentSelectedZone();
			if (zone != null)
			{
				BitmapImage bitmap = new BitmapImage(new Uri(file_path_and_name, UriKind.Absolute));
				Image image = new Image();
				image.Source = bitmap;
				image.Height = bitmap.PixelHeight;
				image.Width = bitmap.PixelWidth;

				ParallaxingBackground newParallaxingBackground = new ParallaxingBackground();

				if (file_path_and_name.Contains(Settings.root_directory))
				{
					file_path_and_name = file_path_and_name.Remove(0, Settings.root_directory.Length);
				}

				newParallaxingBackground.FilePath = file_path_and_name;

				float left = zone.x;
				float top = zone.y;

				newParallaxingBackground.image = image;
				newParallaxingBackground.x = left;
				newParallaxingBackground.y = top;
				newParallaxingBackground.PixelWidth = bitmap.PixelWidth;
				newParallaxingBackground.PixelHeight = bitmap.PixelHeight;
				Canvas.SetLeft(image, newParallaxingBackground.x);
				Canvas.SetTop(image, newParallaxingBackground.y);

				//int item_index = 0;
				string name = "Layer " + zone.ZonesListOfParallaxingBackgrounds.Count;

				//while (GetParallaxingBackground(name) != null)
				//{
				//	item_index++;
				//	name = "Layer " + item_index;
				//}

				newParallaxingBackground.name = name;

				ListBoxItem item = new ListBoxItem();
				item.Content = name;
				item.Selected += OnSelected;
				item.Unselected += OnUnselected;
				ListOfParallaxingBackgrounds.Items.Add(item);
				zone.ZonesListOfParallaxingBackgrounds.Add(item);

				zone.ParallaxingBackgrounds.Add(newParallaxingBackground);
				MyCanvas.Children.Insert(0, image);
			}
		}

		public void LoadParallaxingBackground(ParallaxingBackground parallaxing_background, Zone zone)
		{
			BitmapImage bitmap = new BitmapImage(new Uri(Settings.root_directory + parallaxing_background.FilePath, UriKind.Absolute));

			Image image = new Image();
			image.Source = bitmap;
			image.Height = bitmap.PixelHeight;
			image.Width = bitmap.PixelWidth;

			parallaxing_background.PixelWidth = bitmap.PixelWidth;
			parallaxing_background.PixelHeight = bitmap.PixelHeight;
			parallaxing_background.image = image;

			Canvas.SetLeft(image, parallaxing_background.x);
			Canvas.SetTop(image, parallaxing_background.y);

			MyCanvas.Children.Insert(0, image);

			if (string.IsNullOrWhiteSpace(parallaxing_background.name))
			{
				string name = "Layer " + zone.ZonesListOfParallaxingBackgrounds.Count;
				parallaxing_background.name = name;
			}

			ListBoxItem item = new ListBoxItem();
			item.Content = parallaxing_background.name;
			item.Selected += OnSelected;
			item.Unselected += OnUnselected;
			zone.ZonesListOfParallaxingBackgrounds.Add(item);
		}

		private void TriangleCheckBox_Click(object sender, RoutedEventArgs e)
		{
			SerializedTriangle sTri = GetSerializedTriangle(selected_triangle_name);
			if (sTri != null)
			{
				sTri.right_angle_above_line = TriangleCheckBox.IsChecked.HasValue ? TriangleCheckBox.IsChecked.Value : false;

				if (sTri.right_angle_above_line)
				{
					selected_triangle.Points[1] = new Point(selected_triangle.Points[2].X, selected_triangle.Points[0].Y);
				}
				else
				{
					selected_triangle.Points[1] = new Point(selected_triangle.Points[0].X, selected_triangle.Points[2].Y);
				}

				Canvas.SetLeft(selected_triangle, 0.0f);
				Canvas.SetTop(selected_triangle, 0.0f);
			}
		}

		private void SetRootDirectory_Click(object sender, RoutedEventArgs e)
		{
			SetRootDirectory();
		}

		private void SetRootDirectory()
		{
			using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
			{
				if (Settings.root_directory != "")
				{
					fbd.SelectedPath = Settings.root_directory;
				}

				fbd.Description = "Please select a folder to act as your root folder.";
				System.Windows.Forms.DialogResult result = fbd.ShowDialog();

				if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
				{
					Settings.root_directory = fbd.SelectedPath;
					System.Windows.Forms.MessageBox.Show("Root File Path: " + fbd.SelectedPath, "Message");

					StreamWriter writer = new StreamWriter(levelEditorFileName);

					string jsonStr = JsonConvert.SerializeObject(Settings);

					writer.Write(jsonStr);
					writer.Flush();

					writer.Close();
				}
			}
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			string msg = "Any unsaved progress will be lost.  Do you wish to proceed?";
			MessageBoxResult yesOrNoResult =
			  MessageBox.Show(
				msg,
				"Data App",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (yesOrNoResult == MessageBoxResult.Yes || yesOrNoResult == MessageBoxResult.OK)
			{
				Reset();
			}
		}

		private void Reset()
		{
			mouse_pressed_pos = new Point();
			
			rect_getting_drawn = null;
			triangle_getting_drawn = null;
			line_getting_drawn = null;

			//Polygon triangle_getting_made = null;
			triangles = new Dictionary<string, Polygon>();
			sTris = new List<SerializedTriangle>();
			selected_triangle = null;
			selected_triangle_name = "";

			rail_lines = new Dictionary<string, Line>();
			sRails = new List<SerializedRail>();
			selected_rail = null;
			selected_rail_name = "";

			selected_rectangle = null;
			rectangles = new Dictionary<string, Rectangle>();
			sRects = new List<SerializedRectangle>();
			selected_rect_name = "";

			selected_door = null;
			door_rects = new Dictionary<string, Rectangle>();
			sDoors = new List<SerializedDoor>();
			selected_door_name = "";

			sPlayer = new SerializedPlayer();
			playerRectangle = null;
			playerRectSelected = false;

			ImportedBestiariesFilePaths = new List<string>();
			Bestiaries = new List<Bestiary>();

			//Unit selected_unit = null;
			selected_unit_rectangle = null;
			unit_rects = new Dictionary<string, Rectangle>();
			sUnits = new List<SerializedUnit>();
			selected_unit_name = "";

			selected_trigger = null;
			trigger_rects = new Dictionary<string, Rectangle>();
			sTriggers = new List<SerializedTrigger>();
			selected_trigger_name = "";

			selected_image = null;
			bitmap = new List<BitmapImage>();
			artImages = new List<ArtImage>();
			selected_image_name = "";

			//selected_parallaxing_background = null;
			//parallaxing_background_bitmaps = new List<BitmapImage>();
			//parallaxingBackgrounds = new List<ParallaxingBackground>();
			//selected_parallaxing_background_name = "";

			MyCanvas.Children.Clear();
			ListOfObjects.Items.Clear();
			ListOfObjectTypes.Items.Clear();
			ListOfParallaxingBackgrounds.Items.Clear();

			ListBoxItem rectangleItem = new ListBoxItem();
			rectangleItem.Content = "Rectangle";
			ListOfObjectTypes.Items.Add(rectangleItem);

			ListBoxItem triangleItem = new ListBoxItem();
			triangleItem.Content = "Triangle";
			ListOfObjectTypes.Items.Add(triangleItem);

			ListBoxItem doorItem = new ListBoxItem();
			doorItem.Content = "Door";
			ListOfObjectTypes.Items.Add(doorItem);

			ListBoxItem triggerItem = new ListBoxItem();
			triggerItem.Content = "Trigger";
			ListOfObjectTypes.Items.Add(triggerItem);

			CreatePlayer();
		}

		private void ZonesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ZonesComboBox.SelectedValue == null) { return; }

			string name = ZonesComboBox.SelectedValue.ToString();
			if (name == "--New--")
			{
				string zone_name = "Zone " + ZonesComboBox.Items.Count;

				ComboBoxItem new_combo_box_item = new ComboBoxItem();
				new_combo_box_item.Content = zone_name;
				new_combo_box_item.IsSelected = true;

				ZonesComboBox.Items.Insert(ZonesComboBox.Items.Count - 1, new_combo_box_item);

				zones.Add(new Zone(zone_name));
			}

			ResetParallaxingBackgroundsComboxBox();
		}

		private void ResetParallaxingBackgroundsComboxBox()
		{
			Zone zone = GetCurrentSelectedZone();
			if (zone != null)
			{
				ListOfParallaxingBackgrounds.Items.Clear();

				foreach (var item in zone.ZonesListOfParallaxingBackgrounds)
				{
					ListOfParallaxingBackgrounds.Items.Add(item);
				}
			}
		}

		private Zone GetCurrentSelectedZone()
		{
			foreach (Zone zone in zones)
			{
				if (zone.Name == ZonesComboBox.SelectedValue.ToString())
				{
					return zone;
				}
			}

			return null;
		}
	}

	public class SerializedObject
	{
		public string type;
		public string name;

		public SerializedObject()
		{
		}
	}

	public class SerializedRectangle : SerializedObject
	{
		public float x;
		public float y;
		public float width;
		public float height;
		public float vel_x;
		public float vel_y;
		public bool pass_through;
		public string TiedArtImageFileName;

		public SerializedRectangle()
		{
		}
	}

	public class SerializedTriangle : SerializedObject
	{
		public bool right_angle_above_line;
		public PointCollection points;

		public SerializedTriangle()
		{
		}
	}

	public class SerializedDoor : SerializedRectangle
	{
		public string activator;

		public SerializedDoor()
		{
		}
	}

	public class SerializedTrigger : SerializedRectangle
	{
		public string activity = "";

		public SerializedTrigger()
		{
		}
	}

	public class SerializedRail : SerializedRectangle
	{
		public float x_prime;
		public float y_prime;

		public SerializedRail()
		{
		}
	}

	public class SerializedPlayer : SerializedRectangle
	{
		public SerializedPlayer()
		{
		}
	}

	public class SerializedObjects
	{
		public SerializedPlayer player;
		public List<SerializedRectangle> rectangles;
		public List<SerializedTriangle> triangles;
		public List<SerializedRail> rails;
		public List<SerializedDoor> doors;
		public List<SerializedTrigger> triggers;
		public List<string> bestiaryFilePaths;
		public List<SerializedUnit> units;
		public List<ArtImage> images;
		public List<Zone> zones;
		//public List<ParallaxingBackground> parallaxingBackgrounds;

		public SerializedObjects()
		{
		}
	}

	public class SerializedUnit
	{
		public string UnitType = "";
		public string BestiaryName = "";
		public string InstanceOfUnitName = "";
		public float LevelLocationX = 0.0f;
		public float LevelLocationY = 0.0f;
		public float width = 0.0f;
		public float height = 0.0f;
		public bool IsInteractable = false;
		public bool IsDestructible = false;
		public string activity = "";

		public SerializedUnit()
		{
		}
	}

	public class Bestiary
	{
		public string BestiaryName = "";
		public Dictionary<string, Unit> DictOfUnits = new Dictionary<string, Unit>();

		public Bestiary()
		{
		}
	}

	public class Unit
	{
		public string UnitName = "";
		public int HitPoints = 0;
		public float MovementSpeed = 0.0f;

		public string InstanceOfUnitName = "";
		public float LevelLocationX = 0.0f;
		public float LevelLocationY = 0.0f;
		public float width = 0.0f;
		public float height = 0.0f;
	
		public Unit()
		{
		}
	}

	public class ArtImage
	{
		public string name = "";
		public string FilePath = "";
		public float x;
		public float y;
		public float PixelWidth;
		public float PixelHeight;
		public bool IsStandalone;
		[JsonIgnore]
		public Image image;

		public ArtImage()
		{
		}
	}

	public class ParallaxingBackground
	{
		public string name = "";
		public string FilePath = "";
		private float _x = 0.0f;
		public float x
		{
			get
			{
				return _x;
			}
			set
			{
				_x = value;
				if (image != null)
				{
					Canvas.SetLeft(image, _x);
				}
			}
		}
		private float _y = 0.0f;
		public float y
		{
			get
			{
				return _y;
			}
			set
			{
				_y = value;
				if (image != null)
				{
					Canvas.SetTop(image, _y);
				}
			}
		}
		public float PixelWidth;
		public float PixelHeight;
		[JsonIgnore]
		public Image image;

		public ParallaxingBackground()
		{
		}
	}

	public class Zone
	{
		public string Name = "";
		[JsonIgnore]
		public List<ListBoxItem> ZonesListOfParallaxingBackgrounds = new List<ListBoxItem>();
		public List<ParallaxingBackground> ParallaxingBackgrounds = new List<ParallaxingBackground>();
		private float _x = 0.0f;
		public float x {
			get
			{
				return _x;
			}
			set
			{
				_x = value;
				foreach (ParallaxingBackground pb in ParallaxingBackgrounds)
				{
					pb.x = _x;
				}
			}
		}
		private float _y = 0.0f;
		public float y
		{
			get
			{
				return _y;
			}
			set
			{
				_y = value;
				foreach (ParallaxingBackground pb in ParallaxingBackgrounds)
				{
					pb.y = _y;
				}
			}
		}

		public Zone(string name)
		{
			Name = name;
			x = 0.0f;
			y = 0.0f;
		}

		public ParallaxingBackground GetCurrentlySelectedParallaxingBackground()
		{
			foreach (var item in ZonesListOfParallaxingBackgrounds)
			{
				if (((ListBoxItem)item).IsSelected)
				{
					foreach (ParallaxingBackground pb in ParallaxingBackgrounds)
					{
						if (pb.name == ((ListBoxItem)item).Content.ToString())
						{
							return pb;
						}
					}
				}
			}

			return null;
		}
	}

	public class SerializedSettings
	{
		public string root_directory = "";

		public SerializedSettings()
		{
		}
	}
}
