using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Compiler;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;

using Path = System.IO.Path;
using Color = System.Windows.Media.Color;

namespace WpfApp1;

public partial class MainWindow : Window
{
    private string fileName;
    private string filePath;
    private string filesFolderPath;
    private bool isColorizing = false;
    private DispatcherTimer timer;
    public MainWindow()
    {
        InitializeComponent();
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectDirectory = Directory.GetParent(Directory.GetParent(baseDirectory).FullName).FullName;
        filesFolderPath = Path.Combine(Directory.GetParent(projectDirectory).FullName, "Files"); 
        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += Timer_Tick;
        RCB1.TextChanged += RCB1_TextChanged;
    }


    private void Timer_Tick(object sender, EventArgs e)
    {
        timer.Stop();
        ColorizeRichTextBox(RCB1);
    }

    private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!isColorizing)
        {
            ColorizeRichTextBox(RCB1);
        }
    }

    private void MakeNewFile(object sender, RoutedEventArgs e)
    {
        CreateWindow createFileWindow = new CreateWindow();
        createFileWindow.Closed += CreateFileDialog_Closed;
        createFileWindow.ShowDialog();
    }

    private void CreateFileDialog_Closed(object sender, EventArgs e)
    {
        fileName = ((CreateWindow)sender).FileName;
        filePath = Path.Combine(filesFolderPath, fileName);
        if (!Directory.Exists(filesFolderPath))
        {
            try
            {
                Directory.CreateDirectory(filesFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании папки Files: " + ex.Message);
                return;
            }
        }

        try
        {
            File.WriteAllText(filePath, "Hello");
            RCB1.Document.Blocks.Clear();
            RCB1.Document.Blocks.Add(new Paragraph(new Run("Hello")));

            MessageBox.Show("Файл успешно создан: " + fileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при создании файла: " + ex.Message);
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TextRange textRange = new TextRange(RCB1.Document.ContentStart, RCB1.Document.ContentEnd);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                textRange.Save(fileStream, DataFormats.Text);
            }

            MessageBox.Show("Сохранено успешно");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка сохранения: " + ex.Message);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        RCB1.Undo();

    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        RCB1.Redo();
    }

    private void DeleteAll(object sender, RoutedEventArgs e)
    {
        RCB1.Document.Blocks.Clear();
    }

    private void RCB1_TextChanged(object sender, TextChangedEventArgs e)
    {
        ColorizeRichTextBox(RCB1);
    }

    private void ColorizeRichTextBox(RichTextBox richTextBox)
    {
        if (isColorizing) return;

        isColorizing = true;

        try
        {
            string text = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            TextRange allTextRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            allTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            Regex wordRegex = new Regex(@"(\b\w+\b)|(\W+)", RegexOptions.Compiled);
            MatchCollection matches = wordRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (string.IsNullOrEmpty(match.Value)) continue;
                TextPointer start = richTextBox.Document.ContentStart;
                while (start != null)
                {
                    TextPointerContext result = start.GetPointerContext(LogicalDirection.Forward);

                    if (result == TextPointerContext.Text)
                    {
                        string textInRun = start.GetTextInRun(LogicalDirection.Forward);
                        int index = textInRun.IndexOf(match.Value);
                        if (index >= 0)
                        {
                            TextPointer wordStart = start.GetPositionAtOffset(index);
                            TextPointer wordEnd = wordStart.GetPositionAtOffset(match.Value.Length);
                            if (wordStart != null && wordEnd != null)
                            {
                                string wordWithoutPunctuation = Regex.Replace(match.Value, @"\W", "");
                                Color color = Colors.Red;
                                foreach (var category in Painter.ColorBook.Keys)
                                {
                                    string[] words = category.Split(',');
                                    foreach (string catWord in words)
                                    {
                                        if (catWord.Trim().Equals(wordWithoutPunctuation, StringComparison.OrdinalIgnoreCase))
                                        {
                                            color = Painter.ColorBook[category];
                                            break;
                                        }
                                    }
                                    if (color != Colors.Red) break;
                                }
                                TextRange wordRange = new TextRange(wordStart, wordEnd);
                                wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
                            }
                            break;
                        }
                    }
                    start = start.GetNextContextPosition(LogicalDirection.Forward);
                }
            }
        }
        finally
        {
            isColorizing = false;
        }
    }


    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        TextRange selection = new TextRange(RCB1.Selection.Start, RCB1.Selection.End);
        string selectedText = selection.Text;
        if (!string.IsNullOrEmpty(selectedText))
        {
            Clipboard.SetText(selectedText);
        }
    }

    private void CutButton_Click(object sender, RoutedEventArgs e)
    {
        TextRange selection = new TextRange(RCB1.Selection.Start, RCB1.Selection.End);
        if (!selection.IsEmpty)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                selection.Save(stream, DataFormats.Rtf);
                stream.Seek(0, SeekOrigin.Begin);
                string rtfText = new StreamReader(stream).ReadToEnd();
                Clipboard.SetText(rtfText, TextDataFormat.Rtf);
            }

            selection.Text = "";
        }
    }

    private void PasteButton_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText(TextDataFormat.Rtf))
        {
            try
            {
                string rtfText = Clipboard.GetText(TextDataFormat.Rtf);
                TextRange textRange = new TextRange(RCB1.Selection.Start, RCB1.Selection.End);
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(rtfText)))
                {
                    textRange.Load(stream, DataFormats.Rtf);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при вставке RTF: " + ex.Message);
            }
        }
        else if (Clipboard.ContainsText())
        {
            string clipboardText = Clipboard.GetText();
            RCB1.CaretPosition.InsertTextInRun(clipboardText);
        }
    }


    private void ExitApp(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(filesFolderPath))
        {
            Process.Start("explorer.exe", filesFolderPath);
        }
        else
        {
            MessageBox.Show("Папка не найдена: " + filesFolderPath);
        }
    }

    private void ManualButton_Click(object sender, RoutedEventArgs e)
    {
        TextWindow textWindow = new TextWindow("Справка");
        textWindow.ShowDialog();
    }

    private void AboutProgrammButton_Click(object sender, RoutedEventArgs e)
    {
        TextWindow textWindow = new TextWindow("О программе");
        textWindow.ShowDialog();
    }

    private void TryToExit(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        ExitWindow confirmationWindow = new ExitWindow();
        if (confirmationWindow.ShowDialog() == true)
        {
            e.Cancel = false;
            Application.Current.Shutdown();
        }
    }
}