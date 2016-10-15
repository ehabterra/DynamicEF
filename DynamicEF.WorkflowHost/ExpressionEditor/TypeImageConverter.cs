using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DynamicEF.WorkflowHost.ExpressionEditor
{
    public class TypeImageConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(TreeNodes.NodeTypes))
                return null;

            DrawingBrush result = null;
            object resImage = null;
            var inputValue = (TreeNodes.NodeTypes)Enum.Parse(typeof(TreeNodes.NodeTypes), value.ToString());
            switch (inputValue)
            {
                case TreeNodes.NodeTypes.Namespace:
                    resImage = Application.Current.Resources["ISNamespace"];
                    break;
                case TreeNodes.NodeTypes.Interface:
                    resImage = Application.Current.Resources["ISInterface"];
                    break;
                case TreeNodes.NodeTypes.Class:
                    resImage = Application.Current.Resources["ISClass"];
                    break;
                case TreeNodes.NodeTypes.Method:
                    resImage = Application.Current.Resources["ISMethod"];
                    break;
                case TreeNodes.NodeTypes.Property:
                    resImage = Application.Current.Resources["ISProperty"];
                    break;
                case TreeNodes.NodeTypes.Field:
                    resImage = Application.Current.Resources["ISField"];
                    break;
                case TreeNodes.NodeTypes.Enum:
                    resImage = Application.Current.Resources["ISEnum"];
                    break;
                case TreeNodes.NodeTypes.ValueType:
                    resImage = Application.Current.Resources["ISStructure"];
                    break;
                case TreeNodes.NodeTypes.Event:
                    resImage = Application.Current.Resources["ISEvent"];
                    break;
                case TreeNodes.NodeTypes.Primitive:
                    break;
                default:
                    break;
            }

            if (resImage != null)
            {
                result = new DrawingBrush { Drawing = new ImageDrawing((ImageSource)resImage, new Rect(0, 0, 16, 16)) };
            }
            return result;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}