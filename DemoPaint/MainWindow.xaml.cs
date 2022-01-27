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


        public MainWindow()
        {
            InitializeComponent();

        }

        bool _isDrawing = false;
        List<IShape> _shapes = new List<IShape>();
        IShape _preview;
        string _selectedShapeName = "";
        Dictionary<string, IShape> _prototypes = 
            new Dictionary<string, IShape>();

        private void canvas_MouseDown(object sender, 
            MouseButtonEventArgs e)
        {
            _isDrawing = true;

            Point pos = e.GetPosition(canvas);

            _preview.HandleStart(pos.X, pos.Y);
        }
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
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

                Title = $"{pos.X} {pos.Y}";
                
            }
        }
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
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
            foreach(var shape in _shapes)
            {
                var element = shape.Draw();
                canvas.Children.Add(element);
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

      
        private void colorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
