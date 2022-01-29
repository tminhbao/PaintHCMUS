using Contract;
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
using Line2D;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace DemoPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Brush brushcolor;
        private int diameter = 10;
        private int childcounter = 0;
        public MainWindow()
        {
            InitializeComponent();
            sliderOp.Value = 255;
        }

        // Thêm chức năng Zoom
        private Double zoomMax = 5;
        private Double zoomMin = 0.5;
        private Double zoomSpeed = 0.001;
        private Double zoom = 1;
        private void canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            zoom += zoomSpeed * e.Delta; // Ajust zooming speed (e.Delta = Mouse spin value )
            if (zoom < zoomMin) { zoom = zoomMin; } // Limit Min Scale
            if (zoom > zoomMax) { zoom = zoomMax; } // Limit Max Scale

            Point mousePos = e.GetPosition(canvas);

            if (zoom > 1)
            {
                canvas.RenderTransform = new ScaleTransform(zoom, zoom, mousePos.X, mousePos.Y); // transform Canvas size from mouse position
            }
            else
            {
                canvas.RenderTransform = new ScaleTransform(zoom, zoom); // transform Canvas size
            }
        }

        bool _isDrawing = false;
        List<IShape> _shapes = new List<IShape>();
        IShape _preview;
        string _selectedShapeName = "";
        Dictionary<string, IShape> _prototypes = new Dictionary<string, IShape>();

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (brushCheckbox.IsChecked == true)
                _isDrawing = true;
            else
            {
                _isDrawing = true;
                Point pos = e.GetPosition(canvas);
                _preview.HandleStart(pos.X, pos.Y);
            }
        }
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(brushCheckbox.IsChecked == true)
            {
                Ellipse mybrush = new Ellipse();
                mybrush.Width = diameter;
                mybrush.Height = diameter;
                if (_isDrawing)
                {
                    Point brushPosition = new Point(e.GetPosition(canvas).X, e.GetPosition(canvas).Y);
                    mybrush.Fill = new SolidColorBrush(Color.FromArgb(Convert.ToByte(sliderOp.Value), Convert.ToByte(sliderRed.Value), Convert.ToByte(sliderGreen.Value), Convert.ToByte(sliderBlue.Value)));
                    Canvas.SetTop(mybrush, brushPosition.Y);
                    Canvas.SetLeft(mybrush, brushPosition.X);
                    canvas.Children.Add(mybrush);
                    childcounter++;
                }
            }    
            else if (_isDrawing)
            {
                Point pos = e.GetPosition(canvas);
                _preview.HandleEnd(pos.X, pos.Y);

                // Xoá hết các hình vẽ cũ
                canvas.Children.Clear();

                // Vẽ lại các hình trước đó
                foreach (var shape in _shapes)
                {
                    UIElement element = shape.Draw();
                    canvas.Children.Add(element);
                }

                // Vẽ hình preview đè lên
                canvas.Children.Add(_preview.Draw());

            }
        }
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(brushCheckbox.IsChecked == true)
            {
                _isDrawing = false;
            }
            else
            {
                _isDrawing = false;
                // Thêm đối tượng cuối cùng vào mảng quản lí
                Point pos = e.GetPosition(canvas);
                _preview.HandleEnd(pos.X, pos.Y);
                _shapes.Add(_preview);

                // Sinh ra đối tượng mẫu kế
                _preview = _prototypes[_selectedShapeName].Clone();

                // Ve lai Xoa toan bo
                canvas.Children.Clear();

                // Ve lai tat ca cac hinh
                foreach (var shape in _shapes)
                {
                    var element = shape.Draw();
                    canvas.Children.Add(element);
                }
            } 
                

            
            
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            var dlls = new DirectoryInfo(exeFolder).GetFiles("*.dll");

            foreach (var dll in dlls)
            {
                var assembly = Assembly.LoadFile(dll.FullName);
                var types = assembly.GetTypes();

                foreach(var type in types)
                {
                    if (type.IsClass)
                    {
                        if (typeof(IShape).IsAssignableFrom(type))
                        {
                            var shape = Activator.CreateInstance(type) as IShape;
                            _prototypes.Add(shape.Name, shape);
                        }
                    }
                }
                
            }

            // Tạo ra các nút bấm hàng mẫu
            foreach (var item in _prototypes)
            {
                var shape = item.Value as IShape;

                var button = new Button()
                {
                    Content = shape.Name,
                    Width = 55,
                    Height = 30,
                    Margin = new Thickness(5, 0, 5, 0),
                    Tag = shape.Name
                };
                
                button.Click += prototypeButton_Click;
                prototypesStackPanel.Children.Add(button);
            }

            _selectedShapeName = _prototypes.First().Value.Name;
            _preview = _prototypes[_selectedShapeName].Clone();

        }

        private void prototypeButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedShapeName = (sender as Button).Tag as string;
            _preview = _prototypes[_selectedShapeName];
        }
        // Lưu hình
        private void SaveImage(Canvas canvas, string filename)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            canvas.Measure(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight));
            canvas.Arrange(new Rect(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight)));

            renderBitmap.Render(canvas);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (FileStream file = File.Create(filename))
            {
                encoder.Save(file);
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string relativePath = $"{ AppDomain.CurrentDomain.BaseDirectory}preset\\";
            string path = System.IO.Path.GetFullPath(relativePath);
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = path;
            saveFileDialog.Filter = "Img (*.jpg)|*.jpg";
            if (saveFileDialog.ShowDialog() == true)
            {
                String fileName = saveFileDialog.FileName;
                SaveImage(canvas, fileName);
            }
        }
        // Thêm hình vào Canvas
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            string relativePath = $"{ AppDomain.CurrentDomain.BaseDirectory}preset\\";
            string path = System.IO.Path.GetFullPath(relativePath);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "Image files (*.jpg)|*.jpg|All Files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                _preview = null;
                string filename = openFileDialog.FileName;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filename, UriKind.Absolute);
                bitmap.EndInit();

                Image image = new Image();
                image.Source = bitmap;
                image.Width = bitmap.Width;
                image.Height = bitmap.Height;
                canvas.Children.Add(image);
            }
                
        }
        // Chức năng undo
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            int count = canvas.Children.Count;
            int countShape = 0;
            foreach (var shape in _shapes)
            {
                countShape++;
            }
            _shapes.RemoveAt(countShape - 1);
            canvas.Children.RemoveAt(count-1);            
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _shapes.Clear();
            canvas.Children.Clear();
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            string textContent = textInput.Text;
            TextBlock text = new TextBlock()
            {
                Text = textContent,
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Colors.Yellow),
                FontSize = 20
            };


            canvas.Children.Add(text);
        }

        private void SizeGroupBox_Checked(object sender, RoutedEventArgs e)
        {
            if (radioSmall.IsChecked == true)
                diameter = Convert.ToInt32(2 + sliderSize.Value);
            if (radioMedium.IsChecked == true)
                diameter = Convert.ToInt32(10 + sliderSize.Value);
            if (radioLarge.IsChecked == true)
                diameter = Convert.ToInt32(20 + sliderSize.Value);
        }


    }
}
