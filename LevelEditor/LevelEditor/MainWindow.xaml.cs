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

		SolidColorBrush TransformingColor = Brushes.LightGreen;
		SolidColorBrush SelectedColor = Brushes.Green;
		SolidColorBrush DormantColor = Brushes.LightBlue;
		
		const int OBJECT_SELECTION_RECTANGLE = 0;
		const int OBJECT_SELECTION_TRIANGLE = 1;
		const int OBJECT_SELECTION_DOOR = 2;
		const int OBJECT_SELECTION_TRIGGER = 3;
		const int OBJECT_SELECTION_UNIT_STARTING_INDEX = 4;

		Point mouse_pressed_pos = new Point();

		Polygon triangle_getting_drawn = null;

		//Polygon triangle_getting_made = null;
		Dictionary<string, Polygon> triangles = new Dictionary<string, Polygon>();
		List<SerializedTriangle> sTris = new List<SerializedTriangle>();
		Polygon selected_triangle = null;
		string selected_triangle_name = "";

		Rectangle rect_getting_drawn = null;

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

		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
			Show();

			CreatePlayer();
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
				//triangle_getting_made = new Polygon();
				//triangle_getting_made.StrokeThickness = 0;
				//triangle_getting_made.HorizontalAlignment = HorizontalAlignment.Center;
				//triangle_getting_made.VerticalAlignment = VerticalAlignment.Center;
				//
				//triangle_getting_made.Points = new PointCollection() { mouse_pressed_pos, mouse_pressed_pos, mouse_pressed_pos };

				//Point mouse_pressed_pos_adjusted = new Point(mouse_pressed_pos.X / 2.0f, mouse_pressed_pos.Y / 2.0f);

				triangle_getting_drawn = new Polygon();
				triangle_getting_drawn.Stroke = TransformingColor;
				triangle_getting_drawn.StrokeThickness = 1;
				triangle_getting_drawn.HorizontalAlignment = HorizontalAlignment.Center;
				triangle_getting_drawn.VerticalAlignment = VerticalAlignment.Center;

				triangle_getting_drawn.Points = new PointCollection() { mouse_pressed_pos, mouse_pressed_pos, mouse_pressed_pos };
				//triangle_getting_drawn.Points = new PointCollection() { mouse_pressed_pos_adjusted, mouse_pressed_pos_adjusted, mouse_pressed_pos_adjusted };

				Canvas.SetLeft(triangle_getting_drawn, mouse_pressed_pos.X);
				Canvas.SetTop(triangle_getting_drawn, mouse_pressed_pos.Y);
				//Canvas.SetLeft(triangle_getting_drawn, mouse_pressed_pos_adjusted.X);
				//Canvas.SetTop(triangle_getting_drawn, mouse_pressed_pos_adjusted.Y);
				MyCanvas.Children.Add(triangle_getting_drawn);
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

				//triangle_getting_made.Points[0] = new Point(mouse_pressed_pos.X, mouse_pressed_pos.Y);
				//triangle_getting_made.Points[2] = pos;
				//
				//if (pos.Y < triangle_getting_made.Points[0].Y) {
				//	triangle_getting_made.Points[1] = new Point(triangle_getting_made.Points[2].X, triangle_getting_made.Points[0].Y);
				//} else {
				//	triangle_getting_made.Points[1] = new Point(triangle_getting_made.Points[0].X, triangle_getting_made.Points[2].Y);
				//}

				//pos = e.GetPosition(triangle_getting_drawn);
				//Point mouse_pressed_pos_adjusted = new Point(mouse_pressed_pos.X / 2.0f, mouse_pressed_pos.Y / 2.0f);
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

					float top_left_x = Math.Min((float)mouse_pressed_pos.X, (float)pos.X);
					float top_left_y = Math.Min((float)mouse_pressed_pos.Y, (float)pos.Y);

					SerializedRectangle sRect = new SerializedRectangle();
					sRect.type = "Rectangle";
					sRect.name = name;
					sRect.x = top_left_x;
					sRect.y = top_left_y;
					sRect.width = (float)rect_getting_drawn.Width;
					sRect.height = (float)rect_getting_drawn.Height;

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

					float top_left_x = Math.Min((float)mouse_pressed_pos.X, (float)pos.X);
					float top_left_y = Math.Min((float)mouse_pressed_pos.Y, (float)pos.Y);

					SerializedTrigger sTrigger = new SerializedTrigger();
					sTrigger.type = "Trigger";
					sTrigger.name = name;
					sTrigger.x = top_left_x;
					sTrigger.y = top_left_y;
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

					//MyCanvas.Children.Remove(triangle_getting_drawn);
					//
					//triangle_getting_made.StrokeThickness = 1;
					//MyCanvas.Children.Add(triangle_getting_made);

					triangle_getting_drawn = null;
					//triangle_getting_made = null;
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
				Width = 10.0f,
				Height = 10.0f
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
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideUnitUiItems();
				HideDoorUiItems();
				HideTriggerUiItems();
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
				ShowTriangleUiItems();
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
				wTextBox.IsEnabled = false;
				hTextBox.IsEnabled = false;
				TriangleCheckBox.IsEnabled = false;

				HideTriangleUiItems();
				HideDoorUiItems();
				HideUnitUiItems();
				HideTriggerUiItems();
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
				ShowTriggerUiItems();
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

				HideTriangleUiItems();
				HideDoorUiItems();
				HideRectangleUiItems();
				HideTriggerUiItems();
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

			DeleteButton.IsEnabled = false;

			playerRectSelected = false;

			selected_rect_name = "";
			selected_rectangle = null;

			selected_triangle_name = "";
			selected_triangle = null;

			selected_door_name = "";
			selected_door = null;

			//selected_unit = null;
			selected_unit_name = "";
			selected_unit_rectangle = null;

			selected_trigger_name = "";
			selected_trigger = null;

			xTextBox.Text = "";
			yTextBox.Text = "";
			wTextBox.Text = "";
			hTextBox.Text = "";
			TriangleCheckBox.IsEnabled = false;

			activityTextBox.Text = "";
			doorActivatorTextBox.Text = "";
		}

		private void Apply(object sender, RoutedEventArgs e)
		{
			if (playerRectSelected)
			{
				float.TryParse(xTextBox.Text, out sPlayer.x);
				float.TryParse(yTextBox.Text, out sPlayer.y);

				playerRectangle.Width = sPlayer.width;
				playerRectangle.Height = sPlayer.height;

				MyCanvas.Children.Remove(playerRectangle);

				Canvas.SetLeft(playerRectangle, sPlayer.x);
				Canvas.SetTop(playerRectangle, sPlayer.y);

				MyCanvas.Children.Add(playerRectangle);
			}

			SerializedRectangle sRect = GetSerializedRect(selected_rect_name);
			if (sRect != null)
			{
				float.TryParse(xTextBox.Text, out sRect.x);
				float.TryParse(yTextBox.Text, out sRect.y);
				float.TryParse(wTextBox.Text, out sRect.width);
				float.TryParse(hTextBox.Text, out sRect.height);

				selected_rectangle.Width = sRect.width;
				selected_rectangle.Height = sRect.height;

				MyCanvas.Children.Remove(selected_rectangle);

				Canvas.SetLeft(selected_rectangle, sRect.x);
				Canvas.SetTop(selected_rectangle, sRect.y);

				MyCanvas.Children.Add(selected_rectangle);
			}

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

				sTri.right_angle_above_line = TriangleCheckBox.IsChecked.HasValue ? TriangleCheckBox.IsChecked.Value : false;

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

				MyCanvas.Children.Remove(selected_triangle);

				Canvas.SetLeft(selected_triangle, 0.0f);
				Canvas.SetTop(selected_triangle, 0.0f);

				MyCanvas.Children.Add(selected_triangle);
			}

			SerializedDoor sDoor = GetSerializedDoor(selected_door_name);
			if (sDoor != null)
			{
				float.TryParse(xTextBox.Text, out sDoor.x);
				float.TryParse(yTextBox.Text, out sDoor.y);
				sDoor.activator = doorActivatorTextBox.Text;

				MyCanvas.Children.Remove(selected_door);

				Canvas.SetLeft(selected_door, sDoor.x);
				Canvas.SetTop(selected_door, sDoor.y);

				MyCanvas.Children.Add(selected_door);
			}

			SerializedUnit sUnit = GetSerializedUnit(selected_unit_name);
			if (sUnit != null)
			{
				float.TryParse(xTextBox.Text, out sUnit.LevelLocationX);
				float.TryParse(yTextBox.Text, out sUnit.LevelLocationY);
				sUnit.activity = activityTextBox.Text;

				sUnit.IsInteractable = IsInteractableCheckBox.IsChecked.HasValue ? IsInteractableCheckBox.IsChecked.Value : false;

				MyCanvas.Children.Remove(selected_unit_rectangle);

				Canvas.SetLeft(selected_unit_rectangle, sUnit.LevelLocationX);
				Canvas.SetTop(selected_unit_rectangle, sUnit.LevelLocationY);

				MyCanvas.Children.Add(selected_unit_rectangle);
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

				MyCanvas.Children.Remove(selected_trigger);

				Canvas.SetLeft(selected_trigger, sTrigger.x);
				Canvas.SetTop(selected_trigger, sTrigger.y);

				MyCanvas.Children.Add(selected_trigger);
			}
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
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			SerializedObjects sObjects = new SerializedObjects();
			sObjects.player = sPlayer;
			sObjects.rectangles = sRects;
			sObjects.triangles = sTris;
			sObjects.doors = sDoors;
			sObjects.triggers = sTriggers;
			sObjects.bestiaryFilePaths = ImportedBestiariesFilePaths;
			sObjects.units = sUnits;

			string jsonStr = JsonConvert.SerializeObject(sObjects);

			Stream myStream;
			SaveFileDialog saveFileDialog = new SaveFileDialog();

			saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			saveFileDialog.FilterIndex = 2;
			saveFileDialog.RestoreDirectory = true;

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
				sDoors = sObjects.doors;
				sTriggers = sObjects.triggers;
				ImportedBestiariesFilePaths = sObjects.bestiaryFilePaths;
				sUnits = sObjects.units;

				rectangles.Clear();
				triangles.Clear();
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

				ImportBestiaries();

				isDataDirty = false;
				Deselect();
			}
		}

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

			xLabel.Visibility = Visibility.Visible;
			yLabel.Visibility = Visibility.Visible;
			wLabel.Visibility = Visibility.Visible;
			hLabel.Visibility = Visibility.Visible;
		}

		void HideRectangleUiItems()
		{
			xTextBox.Visibility = Visibility.Hidden;
			yTextBox.Visibility = Visibility.Hidden;
			wTextBox.Visibility = Visibility.Hidden;
			hTextBox.Visibility = Visibility.Hidden;

			xLabel.Visibility = Visibility.Hidden;
			yLabel.Visibility = Visibility.Hidden;
			wLabel.Visibility = Visibility.Hidden;
			hLabel.Visibility = Visibility.Hidden;
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

			System.IO.StreamReader sr = new System.IO.StreamReader(filepath);

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

				if (!ImportedBestiariesFilePaths.Contains(filepath))
				{
					ImportedBestiariesFilePaths.Add(filepath);
				}

				isDataDirty = false;
			}
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
		public List<SerializedDoor> doors;
		public List<SerializedTrigger> triggers;
		public List<string> bestiaryFilePaths;
		public List<SerializedUnit> units;

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
}
