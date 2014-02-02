﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GraphSharp.Controls.Zoom
{
    [TemplatePart(Name = PART_Presenter, Type = typeof(ZoomContentPresenter))]
    public class ZoomControl : ContentControl
    {
        private const string PART_Presenter = "PART_Presenter";

        public static readonly DependencyProperty AnimationLengthProperty =
            DependencyProperty.Register("AnimationLength", typeof(TimeSpan), typeof(ZoomControl),
                                        new UIPropertyMetadata(TimeSpan.FromMilliseconds(500)));

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register("MaxZoom", typeof(double), typeof(ZoomControl), new UIPropertyMetadata(100.0));

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register("MinZoom", typeof(double), typeof(ZoomControl), new UIPropertyMetadata(0.01));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(ZoomControlModes), typeof(ZoomControl),
                                        new UIPropertyMetadata(ZoomControlModes.Custom, ModePropertyChanged));

        private static void ModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            var mode = (ZoomControlModes)e.NewValue;
            switch (mode)
            {
                case ZoomControlModes.Fill:
                    zc.DoZoomToFill();
                    break;
                case ZoomControlModes.Original:
                    zc.DoZoomToOriginal();
                    break;
                case ZoomControlModes.Custom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static readonly DependencyProperty ModifierModeProperty =
            DependencyProperty.Register("ModifierMode", typeof(ZoomViewModifierMode), typeof(ZoomControl),
                                        new UIPropertyMetadata(ZoomViewModifierMode.None));

        public static readonly DependencyProperty TranslateXProperty =
            DependencyProperty.Register("TranslateX", typeof(double), typeof(ZoomControl),
                                        new UIPropertyMetadata(0.0, TranslateXPropertyChanged, TranslateXCoerce));

        public static readonly DependencyProperty TranslateYProperty =
            DependencyProperty.Register("TranslateY", typeof(double), typeof(ZoomControl),
                                        new UIPropertyMetadata(0.0, TranslateYPropertyChanged, TranslateYCoerce));

        public static readonly DependencyProperty ZoomBoxBackgroundProperty =
            DependencyProperty.Register("ZoomBoxBackground", typeof(Brush), typeof(ZoomControl),
                                        new UIPropertyMetadata(null));


        public static readonly DependencyProperty ZoomBoxBorderBrushProperty =
            DependencyProperty.Register("ZoomBoxBorderBrush", typeof(Brush), typeof(ZoomControl),
                                        new UIPropertyMetadata(null));


        public static readonly DependencyProperty ZoomBoxBorderThicknessProperty =
            DependencyProperty.Register("ZoomBoxBorderThickness", typeof(Thickness), typeof(ZoomControl),
                                        new UIPropertyMetadata(null));


        public static readonly DependencyProperty ZoomBoxOpacityProperty =
            DependencyProperty.Register("ZoomBoxOpacity", typeof(double), typeof(ZoomControl),
                                        new UIPropertyMetadata(0.5));


        public static readonly DependencyProperty ZoomBoxProperty =
            DependencyProperty.Register("ZoomBox", typeof(Rect), typeof(ZoomControl),
                                        new UIPropertyMetadata(new Rect()));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(double), typeof(ZoomControl),
                                        new UIPropertyMetadata(1.0, ZoomPropertyChanged));

        private Point _mouseDownPos;
        private ZoomContentPresenter _presenter;

        /// <summary>Applied to the presenter.</summary>
        private ScaleTransform _scaleTransform;
        private Vector _startTranslate;
        private TransformGroup _transformGroup;

        /// <summary>Applied to the scrollviewer.</summary>
        private TranslateTransform _translateTransform;

        private int _zoomAnimCount;
        private bool _isZooming;

        static ZoomControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomControl), new FrameworkPropertyMetadata(typeof(ZoomControl)));
        }

        public ZoomControl()
        {
            PreviewMouseWheel += ZoomControlMouseWheel;
            PreviewMouseDown += (sender1, e1) => OnMouseDown(e1, true);
            MouseDown += (sender, e) => OnMouseDown(e, false);
            MouseUp += ZoomControlMouseUp;
        }

        public Brush ZoomBoxBackground
        {
            get { return (Brush)GetValue(ZoomBoxBackgroundProperty); }
            set { SetValue(ZoomBoxBackgroundProperty, value); }
        }

        public Brush ZoomBoxBorderBrush
        {
            get { return (Brush)GetValue(ZoomBoxBorderBrushProperty); }
            set { SetValue(ZoomBoxBorderBrushProperty, value); }
        }

        public Thickness ZoomBoxBorderThickness
        {
            get { return (Thickness)GetValue(ZoomBoxBorderThicknessProperty); }
            set { SetValue(ZoomBoxBorderThicknessProperty, value); }
        }

        public double ZoomBoxOpacity
        {
            get { return (double)GetValue(ZoomBoxOpacityProperty); }
            set { SetValue(ZoomBoxOpacityProperty, value); }
        }

        public Rect ZoomBox
        {
            get { return (Rect)GetValue(ZoomBoxProperty); }
            set { SetValue(ZoomBoxProperty, value); }
        }

        public Point OrigoPosition
        {
            get { return new Point(ActualWidth / 2, ActualHeight / 2); }
        }

        public double TranslateX
        {
            get { return (double)GetValue(TranslateXProperty); }
            set
            {
                BeginAnimation(TranslateXProperty, null);
                SetValue(TranslateXProperty, value);
            }
        }

        public double TranslateY
        {
            get { return (double)GetValue(TranslateYProperty); }
            set
            {
                BeginAnimation(TranslateYProperty, null);
                SetValue(TranslateYProperty, value);
            }
        }

        public TimeSpan AnimationLength
        {
            get { return (TimeSpan)GetValue(AnimationLengthProperty); }
            set { SetValue(AnimationLengthProperty, value); }
        }

        public double MinZoom
        {
            get { return (double)GetValue(MinZoomProperty); }
            set { SetValue(MinZoomProperty, value); }
        }

        public double MaxZoom
        {
            get { return (double)GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        public double Zoom
        {
            get { return (double)GetValue(ZoomProperty); }
            set
            {
                if (value == (double)GetValue(ZoomProperty))
                    return;
                BeginAnimation(ZoomProperty, null);
                SetValue(ZoomProperty, value);
            }
        }

        protected ZoomContentPresenter Presenter
        {
            get { return _presenter; }
            set
            {
                _presenter = value;
                if (_presenter == null)
                    return;

                //add the ScaleTransform to the presenter
                _transformGroup = new TransformGroup();
                _scaleTransform = new ScaleTransform();
                _translateTransform = new TranslateTransform();
                _transformGroup.Children.Add(_scaleTransform);
                _transformGroup.Children.Add(_translateTransform);
                _presenter.RenderTransform = _transformGroup;
                _presenter.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        /// <summary>Gets or sets the active modifier mode.</summary>
        public ZoomViewModifierMode ModifierMode
        {
            get { return (ZoomViewModifierMode)GetValue(ModifierModeProperty); }
            set { SetValue(ModifierModeProperty, value); }
        }

        /// <summary>Gets or sets the mode of the zoom control.</summary>
        public ZoomControlModes Mode
        {
            get { return (ZoomControlModes)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        private static object TranslateXCoerce(DependencyObject d, object basevalue)
        {
            var zc = (ZoomControl)d;
            return zc.GetCoercedTranslateX((double)basevalue, zc.Zoom);
        }

        private double GetCoercedTranslateX(double baseValue, double zoom)
        {
            if (_presenter == null)
                return 0.0;

            return GetCoercedTranslate(baseValue, zoom,
                                       _presenter.ContentSize.Width,
                                       _presenter.DesiredSize.Width,
                                       ActualWidth);
        }

        private static object TranslateYCoerce(DependencyObject d, object basevalue)
        {
            var zc = (ZoomControl)d;
            return zc.GetCoercedTranslateY((double)basevalue, zc.Zoom);
        }

        private double GetCoercedTranslateY(double baseValue, double zoom)
        {
            if (_presenter == null)
                return 0.0;

            return GetCoercedTranslate(baseValue, zoom,
                                       _presenter.ContentSize.Height,
                                       _presenter.DesiredSize.Height,
                                       ActualHeight);
        }

        /// <summary>Coerces the translation.</summary>
        /// <param name="translate">The desired translate.</param>
        /// <param name="zoom">The factor of the zoom.</param>
        /// <param name="contentSize">The size of the content inside the zoomed ContentPresenter.</param>
        /// <param name="desiredSize">The desired size of the zoomed ContentPresenter.</param>
        /// <param name="actualSize">The size of the ZoomControl.</param>
        /// <returns>The coerced translation.</returns>
        private static double GetCoercedTranslate(double translate, double zoom, double contentSize, double desiredSize, double actualSize)
        {
            /*if (_presenter == null)
                return 0.0;

            //the scaled size of the zoomed content
            var scaledSize = desiredSize * zoom;

            //the plus size above the desired size of the contentpresenter
            var plusSize = contentSize > desiredSize ? (contentSize - desiredSize) * zoom : 0.0;

            //is the zoomed content bigger than actual size of the zoom control?
            /*var bigger = 
                _presenter.ContentSize.Width * zoom > ActualWidth && 
                _presenter.ContentSize.Height * zoom > ActualHeight;*/
            /*var bigger = contentSize * zoom > actualSize;
            var m = bigger ? -1 : 1;

            if (bigger)
            {
                var topRange = m*(actualSize - scaledSize)/2.0;
                var bottomRange = m*((actualSize - scaledSize)/2.0 - plusSize);

                var minusRange = bigger ? bottomRange : topRange;
                var plusRange = bigger ? topRange : bottomRange;

                translate = Math.Max(-minusRange, translate);
                translate = Math.Min(plusRange, translate);
                return translate;
            } else
            {
                return -plusSize/2.0;
            }*/
            return translate;
        }

        private void ZoomControlMouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (ModifierMode)
            {
                case ZoomViewModifierMode.None:
                    return;
                case ZoomViewModifierMode.Pan:
                    break;
                case ZoomViewModifierMode.ZoomBox:
                    ZoomTo(ZoomBox);
                    break;
            }

            ModifierMode = ZoomViewModifierMode.None;
            PreviewMouseMove -= ZoomControlPreviewMouseMove;
            ReleaseMouseCapture();
        }

        public void ZoomTo(Rect rect)
        {
            var deltaZoom = Math.Min(
                ActualWidth / rect.Width,
                ActualHeight / rect.Height);

            var startHandlePosition = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            DoZoom(deltaZoom, OrigoPosition, startHandlePosition, OrigoPosition);
            ZoomBox = new Rect();
        }

        private void ZoomControlPreviewMouseMove(object sender, MouseEventArgs e)
        {
            switch (ModifierMode)
            {
                case ZoomViewModifierMode.None:
                    return;
                case ZoomViewModifierMode.Pan:
                    var translate = _startTranslate + (e.GetPosition(this) - _mouseDownPos);
                    TranslateX = translate.X;
                    TranslateY = translate.Y;
                    break;
                case ZoomViewModifierMode.ZoomBox:
                    var pos = e.GetPosition(this);
                    var x = Math.Min(_mouseDownPos.X, pos.X);
                    var y = Math.Min(_mouseDownPos.Y, pos.Y);
                    var sizeX = Math.Abs(_mouseDownPos.X - pos.X);
                    var sizeY = Math.Abs(_mouseDownPos.Y - pos.Y);
                    ZoomBox = new Rect(x, y, sizeX, sizeY);
                    break;
            }
        }

        private void OnMouseDown(MouseButtonEventArgs e, bool isPreview)
        {
            if (ModifierMode != ZoomViewModifierMode.None)
                return;

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.None:
                    if (!isPreview)
                        ModifierMode = ZoomViewModifierMode.Pan;
                    break;
                case ModifierKeys.Alt:
                    ModifierMode = ZoomViewModifierMode.ZoomBox;
                    break;
                case ModifierKeys.Control:
                    break;
                case ModifierKeys.Shift:
                    ModifierMode = ZoomViewModifierMode.Pan;
                    break;
                case ModifierKeys.Windows:
                    break;
                default:
                    return;
            }

            if (ModifierMode == ZoomViewModifierMode.None)
                return;

            _mouseDownPos = e.GetPosition(this);
            _startTranslate = new Vector(TranslateX, TranslateY);
            Mouse.Capture(this);
            PreviewMouseMove += ZoomControlPreviewMouseMove;
        }

        private static void TranslateXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            if (zc._translateTransform == null)
                return;
            zc._translateTransform.X = (double)e.NewValue;
            if (!zc._isZooming)
                zc.Mode = ZoomControlModes.Custom;
        }

        private static void TranslateYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            if (zc._translateTransform == null)
                return;
            zc._translateTransform.Y = (double)e.NewValue;
            if (!zc._isZooming)
                zc.Mode = ZoomControlModes.Custom;
        }

        private static void ZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;

            if (zc._scaleTransform == null)
                return;

            var zoom = (double)e.NewValue;
            zc._scaleTransform.ScaleX = zoom;
            zc._scaleTransform.ScaleY = zoom;
            if (!zc._isZooming)
            {
                double delta = (double)e.NewValue / (double)e.OldValue;
                zc.TranslateX *= delta;
                zc.TranslateY *= delta;
                zc.Mode = ZoomControlModes.Custom;
            }
        }

        private void ZoomControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var origoPosition = new Point(ActualWidth / 2, ActualHeight / 2);
            Point mousePosition = e.GetPosition(this);

            var deltaZoom = Math.Max(0.2, Math.Min(2.0, e.Delta / 300.0 + 1));
            DoZoom(
                deltaZoom,
                origoPosition,
                mousePosition,
                mousePosition);
        }

        private void DoZoom(double deltaZoom, Point origoPosition, Point startHandlePosition, Point targetHandlePosition)
        {
            double startZoom = Zoom;
            double currentZoom = startZoom * deltaZoom;
            currentZoom = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoom));

            var startTranslate = new Vector(TranslateX, TranslateY);

            var v = (startHandlePosition - origoPosition);
            var vTarget = (targetHandlePosition - origoPosition);

            var targetPoint = (v - startTranslate) / startZoom;
            var zoomedTargetPointPos = targetPoint * currentZoom + startTranslate;
            var endTranslate = vTarget - zoomedTargetPointPos;

            double transformX = GetCoercedTranslateX(TranslateX + endTranslate.X, currentZoom);
            double transformY = GetCoercedTranslateY(TranslateY + endTranslate.Y, currentZoom);

            DoZoomAnimation(currentZoom, transformX, transformY);
            Mode = ZoomControlModes.Custom;
        }

        private void DoZoomAnimation(double targetZoom, double transformX, double transformY)
        {
            _isZooming = true;
            var duration = new Duration(AnimationLength);
            StartAnimation(TranslateXProperty, transformX, duration);
            StartAnimation(TranslateYProperty, transformY, duration);
            StartAnimation(ZoomProperty, targetZoom, duration);
        }

        private void StartAnimation(DependencyProperty dp, double toValue, Duration duration)
        {
            if (double.IsNaN(toValue) || double.IsInfinity(toValue))
            {
                if (dp == ZoomProperty)
                {
                    _isZooming = false;
                }
                return;
            }
            var animation = new DoubleAnimation(toValue, duration);
            if (dp == ZoomProperty)
            {
                _zoomAnimCount++;
                animation.Completed += (s, args) =>
                                           {
                                               _zoomAnimCount--;
                                               if (_zoomAnimCount > 0)
                                                   return;
                                               var zoom = Zoom;
                                               BeginAnimation(ZoomProperty, null);
                                               SetValue(ZoomProperty, zoom);
                                               _isZooming = false;
                                           };
            }
            BeginAnimation(dp, animation, HandoffBehavior.Compose);
        }

        public void ZoomToOriginal()
        {
            Mode = ZoomControlModes.Original;
        }

        private void DoZoomToOriginal()
        {
            if (_presenter == null)
                return;

            var initialTranslate = GetInitialTranslate();
            DoZoomAnimation(1.0, initialTranslate.X, initialTranslate.Y);
        }

        private Vector GetInitialTranslate()
        {
            if (_presenter == null)
                return new Vector(0.0, 0.0);
            var w = _presenter.ContentSize.Width - _presenter.DesiredSize.Width;
            var h = _presenter.ContentSize.Height - _presenter.DesiredSize.Height;
            var tX = -w / 2.0;
            var tY = -h / 2.0;

            return new Vector(tX, tY);
        }

        public void ZoomToFill()
        {
            Mode = ZoomControlModes.Fill;
        }

        private void DoZoomToFill()
        {
            if (_presenter == null)
                return;

            var deltaZoom = Math.Min(
                ActualWidth / _presenter.ContentSize.Width,
                ActualHeight / _presenter.ContentSize.Height);

            var initialTranslate = GetInitialTranslate();
            DoZoomAnimation(deltaZoom, initialTranslate.X * deltaZoom, initialTranslate.Y * deltaZoom);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //get the presenter, and initialize
            Presenter = GetTemplateChild(PART_Presenter) as ZoomContentPresenter;
            if (Presenter != null)
            {
                Presenter.SizeChanged += (s, a) =>
                                             {
                                                 if (Mode == ZoomControlModes.Fill)
                                                     DoZoomToFill();
                                             };
                Presenter.ContentSizeChanged += (s, a) =>
                {
                    if (Mode == ZoomControlModes.Fill)
                        DoZoomToFill();
                };
            }
            ZoomToFill();
        }
    }
}