using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using LoadingDemo.Controls;

namespace LoadingDemo.Extensions
{
    public static class FrameworkElementExtension
    {
        #region Properties

        #region IsLoading
        public static bool GetIsLoading(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLoadingProperty);
        }

        public static void SetIsLoading(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.RegisterAttached("IsLoading", typeof(bool), typeof(FrameworkElementExtension), new PropertyMetadata(IsLoadingPropertyChangedCallback));

        private static void IsLoadingPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement control)
            {
                if (!control.IsLoaded)
                {
                    control.Loaded -= Control_Loaded;
                    control.Loaded += Control_Loaded;
                }
                else
                {
                    control.Loading((bool)e.NewValue);
                }
            }
        }

        private static void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement control)
                control.Loading(GetIsLoading(control));
        }

        #endregion

        #region MaskContent
        public static object GetMaskContent(DependencyObject obj)
        {
            return obj.GetValue(MaskContentProperty);
        }

        public static void SetMaskContent(DependencyObject obj, object value)
        {
            obj.SetValue(MaskContentProperty, value);
        }

        public static readonly DependencyProperty MaskContentProperty =
            DependencyProperty.RegisterAttached("MaskContent", typeof(object), typeof(FrameworkElementExtension));

        #endregion

        #endregion

        #region Methods
        public static void Loading(this FrameworkElement element, bool isOpen = true)
        {
            if (element == null) return;
            //移除遮罩
            if (!isOpen)
            {
                ClearAdorners(element);
                return;
            }

            //获取遮罩元素
            var maskContent = GetMaskContent(element);
            if (maskContent == null)
            {
                maskContent = element.TryFindResource("MaskContent");
                if (maskContent == null)
                {
                    maskContent = new Loading()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }

            //获取装饰层
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);
            if (adornerLayer == null)
            {
                throw new Exception("未找到装饰层。");
            }

            //添加遮罩层
            if (maskContent is FrameworkElement maskElement)
            {
                if (maskElement.Parent != null)
                {
                    var type = maskContent.GetType();
                    var properties = type.GetProperties().Where(p => p.CanWrite).ToArray();
                    var obj = Activator.CreateInstance(type);
                    foreach (var property in properties)
                    {
                        if (property.Name == "Content" || property.Name == "Child" || property.Name == "Children")
                            continue;
                        property.SetValue(obj, property.GetValue(maskContent));
                    }
                    maskContent = obj;
                }
            }
            adornerLayer.Add(new MaskAdorner(element, maskContent));
        }

        private static void ClearAdorners(FrameworkElement element)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);
            if (adornerLayer != null)
            {
                Adorner[] adorners = adornerLayer.GetAdorners(element);
                if (adorners != null)
                {
                    for (int i = 0; i < adorners.Length; i++)
                    {
                        if (adorners[i] is IDisposable disposable)
                            disposable.Dispose();
                        adornerLayer.Remove(adorners[i]);
                    }
                }
            }
        }
        #endregion
    }

    public class MaskAdorner : Adorner, IDisposable
    {
        private FrameworkElement _owner;
        private VisualCollection _child;
        private Border _border;
        private ContentControl _content;

        public MaskAdorner(FrameworkElement owner, object content, Action completed = null) : base(owner)
        {
            _owner = owner;
            _owner.SizeChanged += (sender, e) => this.InvalidateVisual();

            _content = new ContentControl
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _border = new Border
            {
                Background = new SolidColorBrush() { Color = (Color)ColorConverter.ConvertFromString("#B2000000") },
                Child = _content
            };
            _child = new VisualCollection(this) { _border };

            var storyboard = new Storyboard();
            if (completed != null)
                storyboard.Completed += (sender, e) => completed();
            AddDoubleAnimationUsingKeyFrames(storyboard, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)", _content);
            AddDoubleAnimationUsingKeyFrames(storyboard, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)", _content);
            AddRenderTransform(_content);
            AddTrigger(_content, storyboard);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_border != null)
            {
                _border.Arrange(new Rect(new Point(0, 0), new Size(_owner.ActualWidth, _owner.ActualHeight)));
            }
            return finalSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (_border != null)
            {
                _border.Arrange(new Rect(new Point(0, 0), new Size(_owner.ActualWidth, _owner.ActualHeight)));
            }
            return base.MeasureOverride(constraint);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _child == null ? null : _child[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return _child == null ? 0 : _child.Count;
            }
        }

        private void AddRenderTransform(FrameworkElement element)
        {
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform() { ScaleX = 0, ScaleY = 0 });
            group.Children.Add(new SkewTransform());
            group.Children.Add(new RotateTransform());
            group.Children.Add(new TranslateTransform());

            element.RenderTransform = group;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void AddTrigger(FrameworkElement target, Storyboard storyboard)
        {
            var trigger = new EventTrigger { RoutedEvent = LoadedEvent };
            var beginStoryboard = new BeginStoryboard() { Storyboard = storyboard };
            trigger.Actions.Add(beginStoryboard);
            target.Triggers.Add(trigger);
        }

        private void AddDoubleAnimationUsingKeyFrames(Storyboard storyboard, string property, FrameworkElement target)
        {
            var daukf = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTargetProperty(daukf, new PropertyPath(property));
            Storyboard.SetTarget(daukf, target);

            var edkf = new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = 0
            };
            daukf.KeyFrames.Add(edkf);

            var quinticEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            edkf = new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6)),
                Value = 1,
                EasingFunction = quinticEase
            };

            daukf.KeyFrames.Add(edkf);
            storyboard.Children.Add(daukf);
        }

        public void Dispose()
        {
            _content.Content = null;
        }
    }
}
