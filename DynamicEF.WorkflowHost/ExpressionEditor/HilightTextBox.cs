using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Media;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{
    /// <summary>
    /// Syntax Highlight TextBox
    /// </summary>
    /// <remarks>http://blogs.microsoft.co.il/blogs/tamir/archive/2006/12/14/RichTextBox-syntax-highlighting.aspx</remarks>
    public class HilightTextBox : RichTextBox
    {
        #region "変数 Variables"

        private Regex keyWords;
        private Regex stringMatch = new Regex("\\\"(.*?)\\\"");

        private List<WordTag> wordTagList = new List<WordTag>();
        private char[] chrs = {
            '.',
            ')',
            '(',
            '[',
            ']',
            '>',
            '<',
            ':',
            ';',
            '\r',
            '\n',
            '\t'
        };
        private List<char> specials = new List<char>();
        #endregion

        #region "プロパティ Property"

        internal string HighlightWords
        {
            set
            {
                if (value == null)
                {
                    keyWords = null;
                }
                else
                {
                    keyWords = new Regex(value);
                }
            }
        }
        internal TreeNodes IntellisenceList { get; set; }

        #region "テキストボックス互換用プロパティ Like Textbox Control/s Property"

        public int MaxLines { get; set; }
        public int MinLines { get; set; }

        public string Text { get; set; }

        public int SelectionStart { get; set; }
        #endregion

        #endregion

        #region "コンストラクタ New-Finalize"
        public HilightTextBox()
        {
            specials.AddRange(chrs);
        }
        #endregion

        #region "メソッド Method"
        //Protected Overrides Sub OnRender(drawingContext As System.Windows.Media.DrawingContext)

        //    drawingContext.PushClip(New RectangleGeometry(New Rect(0, 0, Me.ActualWidth, Me.ActualHeight)))
        //    drawingContext.DrawRectangle(Brushes.White, Nothing, New Rect(0, 0, Me.ActualWidth, Me.ActualHeight))
        //    If Me.Text.Trim = "" Then Return


        //    Dim edtText = Me.Text.ToLower
        // Dim flowDir As FlowDirection = Windows.FlowDirection.LeftToRight
        // If Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft Then
        //        flowDir = Windows.FlowDirection.RightToLeft
        // End If
        //    Dim fmtText As New FormattedText(edtText, Globalization.CultureInfo.CurrentCulture,
        //                                      flowDir,
        //                                      New Typeface(Me.FontFamily.Source),
        //                                      Me.FontSize,
        //                                      Brushes.Black) With {.Trimming = TextTrimming.None
        //}

        //    //既存クラスの検索
        //    //キーワードの検索
        //    If keyWords IsNot Nothing Then
        //        Tasks.Parallel.ForEach(keyWords.Matches(edtText).Cast(Of Match),
        //                       Sub(keyWordMatch As Match)
        //                           Dim st = edtText.IndexOf(keyWordMatch.ToString.ToLower)
        // fmtText.SetForegroundBrush(Brushes.Blue, st, keyWordMatch.ToString.Length)
        // End Sub)
        //    End If
        //    //文字列の検索
        //    Tasks.Parallel.ForEach(stringMatch.Matches(edtText).Cast(Of Match),
        //                   Sub(keyWordMatch As Match)
        //                       Dim st = edtText.IndexOf(keyWordMatch.ToString.ToLower)
        // fmtText.SetForegroundBrush(Brushes.Red, st, keyWordMatch.ToString.Length)
        // End Sub)

        //    drawingContext.DrawText(fmtText, New Point(Me.Margin.Left, Me.Margin.Top))
        //    MyBase.OnRender(drawingContext)

        //End Sub

        private void OnTextChangedPrivate(object sender, TextChangedEventArgs e)
        {
            if (this.Document == null) return;

            var docRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
            docRange.ClearAllProperties();

            var navigator = this.Document.ContentStart;

            while (navigator.CompareTo(this.Document.ContentEnd) < 0)
            {
                var context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart &&
                   navigator.Parent.GetType() == typeof(Run))
                {
                    CheckWordsInRun(navigator.Parent as Run);
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
            FormatText();
        }

        private void FormatText()
        {
            this.TextChanged -= OnTextChangedPrivate;

            System.Threading.Tasks.Parallel.ForEach(wordTagList.ToList(), (target) =>
                                                          {
                                                              var range = new TextRange(target.StartPosition, target.EndPosition);
                                                              range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                                                          });
            wordTagList.Clear();

            this.TextChanged += OnTextChangedPrivate;
        }

        private void CheckWordsInRun(Run runSection)
        {
            string text = runSection.Text;

            int stIndex = 0;
            int edIndex = 0;
            for (int index = 0; index <= text.Length; index++)
            {
                if (!(char.IsWhiteSpace(text[index]) || (specials.Contains(text[index]))))
                {
                    if ((index > 0) && (!char.IsWhiteSpace(text[index - 1]) || specials.Contains(text[index - 1])))
                    {
                        edIndex = index - 1;
                        var word = text.Substring(stIndex, edIndex - stIndex + 1);


                    }
                }

            }
        }

        #region "内部利用クラス InPrivate Structure"
        internal struct WordTag
        {
            public TextPointer StartPosition;
            public TextPointer EndPosition;
            public string Word;
        }
        #endregion

        #endregion

    }
}