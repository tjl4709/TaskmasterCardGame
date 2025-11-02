using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Backend;

namespace GUI
{
    public class GuiCardFormatter : ICardFormatter
    {
        public CardContentEditor ContentEditor;

        public GuiCardFormatter()
        {
        }

        public override string Prompt(string description, FormatTypes type)
        {
            ContentEditor?.InsertCustomizableAtCursor(description, type);
            return "";
        }
    }


    /// <summary>
    /// Interaction logic for CardContentEditor.xaml
    /// </summary>
    public partial class CardContentEditor : UserControl
    {
        public int CursorIndex {
            get;
            protected set;
        }

        public string Text {
            get {
                string text = "";
                foreach (TextBox child in CardContentWrapPanel.Children) {
                    // Customizables have their Tag set to the FormatType they're meant to parse
                    if (Customizing || child.Tag == null) {
                        text += $" {child.Text}";
                    } else {
                        // text box will be in format "user-entered description:type"
                        int colon_index = child.Text.LastIndexOf(":");
                        string customizable = child.Text.Substring(0, colon_index + 1);
                        customizable += char.ToLower(child.Text[colon_index + 1]);
                        text += $"<<{customizable}>>";
                    }
                }
                return text.Trim();
            }
            set {
                CardContentWrapPanel.Children.Clear();
                CursorIndex = 0;

                if (string.IsNullOrWhiteSpace(value)) {
                    // if set to no text, create one blank label
                    AddLabel(false);
                } else {
                    // otherwise, create labels and customizables based on set text
                    string customizable;
                    int index;
                    while (!string.IsNullOrWhiteSpace(value)) {
                        if (value.StartsWith(ICardFormatter.OPENING)) {
                            index = value.IndexOf(ICardFormatter.CLOSING);
                            if (index == -1)
                                throw new FormatException("unclosed customizable");
                            customizable = value.Substring(ICardFormatter.OPENING.Length, index - ICardFormatter.OPENING.Length);
                            value = value.Substring(index + ICardFormatter.CLOSING.Length).TrimStart();
                            index = customizable.IndexOf(':');
                            if (index != -1 && index != customizable.Length - 2) {
                                customizable = customizable.Substring(0, index + 2);
                            }
                            m_cardFormatter.Format(customizable);
                        } else {    // normal token
                            var label = AddLabel(true);
                            index = value.IndexOf(' ');
                            if (index == -1) {
                                label.Text = value;
                                value = "";
                            } else {
                                label.Text = value.Substring(0, index);
                                value = value.Substring(index + 1).TrimStart();
                            }
                        }
                    }   // end while
                }
            }
        }

        public bool Customizing { get; protected set; }
        protected GuiCardFormatter m_cardFormatter;


        public CardContentEditor() : this("", null) { }
        public CardContentEditor(string text, GuiCardFormatter formatter = null)
            : base()
        {
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(text)) {
                InsertLabel(0, true);
            } else {
                Text = text;
            }

            if (Customizing = formatter != null) {
                m_cardFormatter = formatter;
                m_cardFormatter.ContentEditor = this;
            }
        }


        public void SetCustomizing(GuiCardFormatter formatter = null)
        {
            Customizing = true;
            m_cardFormatter = formatter ?? new GuiCardFormatter();
            m_cardFormatter.ContentEditor = this;
        }

        public TextBox InsertCustomizableAtCursor(string description, FormatTypes type)
        {
            var customizable = new TextBox {
                MinWidth = 10
            };
            string text = $"{description}:{type}";
            if (Customizing) {
                if (TextBoxHelper.GetOrCreateAdorner(customizable, out TextBoxHelper.PlaceholderAdorner adorner)) {
                    TextBoxHelper.SetPlaceholder(customizable, text);
                    customizable.Tag = type;
                    customizable.TextChanged += VerifyTextMatchesFormat;
                } else {
                    customizable = null;
                }
            } else {
                customizable.Text = text;
                customizable.GotFocus += TextBox_GotFocus;
                customizable.PreviewKeyDown += DoSpecialTraversal;
                customizable.TextChanged += VerifyTextHasValidFormatType;
            }

            if (customizable != null) {
                CardContentWrapPanel.Children.Insert(CursorIndex, customizable);
                customizable.CaretIndex = customizable.Text.Length;
                customizable.Focus();
            }
            return customizable;
        }

        private TextBox AddLabel(bool setCursor) { return InsertLabel(CardContentWrapPanel.Children.Count, setCursor); }
        
        private TextBox InsertLabel(int index, bool setCursor)
        {
            var label = new TextBox {
                BorderBrush = Brushes.Transparent,
                MinWidth = 10,
                Tag = null
            };
            
            label.PreviewKeyDown += DoSpecialTraversal;
            label.GotFocus += TextBox_GotFocus;
            CardContentWrapPanel.Children.Insert(index, label);
            if (setCursor) {
                CursorIndex = index;
            }
            return label;
        }

        private void DoSpecialTraversal(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            bool isCustomizable = textBox.Tag != null;
            switch (e.Key) {
                case Key.Back:
                    // if backspacing from the front of a token, combine it with the previous
                    if (textBox.SelectionLength == 0 && textBox.CaretIndex == 0 && CursorIndex != 0) {
                        CardContentWrapPanel.Children.RemoveAt(CursorIndex);
                        var previous = (TextBox)CardContentWrapPanel.Children[--CursorIndex];
                        int previousCaret = previous.Text.Length;
                        previous.Text += textBox.Text;
                        previous.CaretIndex = previousCaret;
                        previous.Focus();
                        e.Handled = true;
                    }
                    break;
                case Key.Delete:
                    // if deleting from the end of a token, combine it with the next
                    if (textBox.SelectionLength == 0 && textBox.CaretIndex == textBox.Text.Length && CursorIndex < CardContentWrapPanel.Children.Count - 1) {
                        CardContentWrapPanel.Children.RemoveAt(CursorIndex);
                        var next = (TextBox)CardContentWrapPanel.Children[CursorIndex];
                        next.Text = textBox.Text + next.Text;
                        next.CaretIndex = textBox.Text.Length;
                        next.Focus();
                        e.Handled = true;
                    }
                    break;
                case Key.Left:
                    // if moving left from the front of a token, move the Caret to the end of the previous
                    if (textBox.SelectionLength == 0 && textBox.CaretIndex == 0 && CursorIndex != 0) {
                        var previous = (TextBox)CardContentWrapPanel.Children[--CursorIndex];
                        previous.CaretIndex = previous.Text.Length;
                        previous.Focus();
                        e.Handled = true;
                    }
                    break;
                case Key.Right:
                    // if moving right from the end of a tokan, move the Caret to the start of the next
                    if (textBox.SelectionLength == 0 && textBox.CaretIndex == textBox.Text.Length && CursorIndex < CardContentWrapPanel.Children.Count - 1) {
                        var next = (TextBox)CardContentWrapPanel.Children[++CursorIndex];
                        next.CaretIndex = 0;
                        next.Focus();
                        e.Handled = true;
                    }
                    break;
                case Key.Space:
                    // space will break the current token into two at the Caret and move it to the start of
                    // the second newly created token. Customizables can have spaces in them, but hitting space
                    // at the end of one should still create a new blank item after it.
                    if (!isCustomizable || textBox.CaretIndex == textBox.Text.Length) {
                        if (textBox.SelectionLength > 0) {
                            int caret = textBox.SelectionStart;
                            textBox.Text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
                            textBox.CaretIndex = caret;
                        }
                        TextBox newLabel = InsertLabel(++CursorIndex, false);
                        newLabel.Text = textBox.Text.Substring(textBox.CaretIndex);
                        textBox.Text = textBox.Text.Substring(0, textBox.CaretIndex);
                        newLabel.CaretIndex = 0;
                        newLabel.Focus();
                        e.Handled = true;
                    }
                    break;
                default:
                    // Do nothing, let the underlying event handler handle it
                    break;
            }
        }

        private void VerifyTextMatchesFormat(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            if (Customizing) {
                if (!m_cardFormatter.Verify(textBox.Text, (FormatTypes)textBox.Tag))
                    throw new FormatException($"Failed to parse \"{textBox.Text}\" as {(FormatTypes)textBox.Tag}");
            }
        }

        private void VerifyTextHasValidFormatType(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            if (!Customizing) {
                string formatType = textBox.Text.Substring(textBox.Text.LastIndexOf(':') + 1);
                Enum.Parse(typeof(FormatTypes), formatType);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CursorIndex = CardContentWrapPanel.Children.IndexOf((UIElement)sender);
        }

        private void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var lastChild = (TextBox)CardContentWrapPanel.Children[CardContentWrapPanel.Children.Count - 1];
            lastChild.Focus();
            lastChild.CaretIndex = lastChild.Text.Length;
        }
    }
}
