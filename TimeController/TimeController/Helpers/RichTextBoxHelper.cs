using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TimeController.Helpers
{
    public static class RichTextBoxHelper
    {
        public static readonly DependencyProperty BindableDocumentProperty = DependencyProperty.RegisterAttached(
            "BindableDocument",
            typeof(FlowDocument),
            typeof(RichTextBoxHelper),
            new PropertyMetadata(null, OnBindableDocumentChanged));

        public static FlowDocument GetBindableDocument(DependencyObject obj)
        {
            return (FlowDocument)obj.GetValue(BindableDocumentProperty);
        }

        public static void SetBindableDocument(DependencyObject obj, FlowDocument value)
        {
            obj.SetValue(BindableDocumentProperty, value);
        }

        private static void OnBindableDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox box)
            {
                box.Document = e.NewValue as FlowDocument;
            }
        }
    }
}