﻿using CoreAnimation;
using CoreGraphics;
using Foundation;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using XF.Material.Forms.UI;
using XF.Material.iOS.Delegates;
using XF.Material.iOS.Renderers;

[assembly: ExportRenderer(typeof(MaterialButton), typeof(MaterialButtonRenderer))]

namespace XF.Material.iOS.Renderers
{
    public class MaterialButtonRenderer : ButtonRenderer
    {
        private const int PRESSED_ELEVATION = 8;
        private const int RESTING_ELEVATION = 2;
        private CALayer _animationLayer;
        private UIColor _borderColor;
        private CABasicAnimation _colorPressed;
        private CABasicAnimation _colorResting;
        private UIColor _disabledBackgroundColor;
        private UIColor _disabledBorderColor;
        private UIColor _disabledTextColor;
        private MaterialButton _materialButton;
        private UIColor _normalTextColor;
        private UIColor _pressedBackgroundColor;
        private UIColor _restingBackgroundColor;
        private UIColor _rippleColor;
        private CABasicAnimation _shadowOffsetPressed;
        private CABasicAnimation _shadowOffsetResting;
        private CABasicAnimation _shadowRadiusPressed;
        private CABasicAnimation _shadowRadiusResting;
        private bool _withIcon;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            this.UpdateLayerFrame();

            if (_materialButton.WidthRequest == -1 && _materialButton.Width < 64)
            {
                _materialButton.WidthRequest = 64;
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
        {
            base.OnElementChanged(e);

            if (e?.OldElement != null)
            {
                this.Control.TouchDown -= this.Control_Pressed;
                this.Control.TouchDragEnter -= this.Control_Pressed;
                this.Control.TouchUpInside -= this.Control_Released;
                this.Control.TouchCancel -= this.Control_Released;
                this.Control.TouchDragExit -= this.Control_Released;
            }

            if (e?.NewElement != null)
            {
                _materialButton = this.Element as MaterialButton;
                _withIcon = _materialButton.Image != null;

                if (_materialButton.AllCaps)
                {
                    _materialButton.Text = _materialButton.Text?.ToUpper();
                }

                this.SetupIcon();
                this.SetupColors();
                this.CreateStateAnimations();
                this.UpdateButtonLayer();
                this.UpdateCornerRadius();
                this.UpdateState();
                this.Control.TouchDown += this.Control_Pressed;
                this.Control.TouchDragEnter += this.Control_Pressed;
                this.Control.TouchUpInside += this.Control_Released;
                this.Control.TouchCancel += this.Control_Released;
                this.Control.TouchDragExit += this.Control_Released;
            }
        }

        protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(Button.IsEnabled))
            {
                this.UpdateState();
            }

            if (e?.PropertyName == MaterialButton.MaterialButtonColorChanged || e?.PropertyName == nameof(MaterialButton.ButtonType))
            {
                this.SetupColors();
                this.CreateStateAnimations();
                this.UpdateButtonLayer();
                await this.UpdateBackgroundColor();
            }

            if (e?.PropertyName == nameof(MaterialButton.Image))
            {
                this.SetupIcon();
                this.UpdateButtonLayer();
            }

            if (e?.PropertyName == nameof(MaterialButton.AllCaps))
            {
                _materialButton.Text = _materialButton.AllCaps ? _materialButton.Text.ToUpper() : _materialButton.Text.ToLower();
            }

            if (e?.PropertyName == nameof(MaterialButton.CornerRadius))
            {
                this.UpdateCornerRadius();
            }
        }

        private async void Control_Pressed(object sender, EventArgs e)
        {
            if (_materialButton.ButtonType == MaterialButtonType.Elevated)
            {
                await AnimateAsync(0.150, () =>
                {
                    this.Control.Layer.AddAnimation(_shadowOffsetPressed, "shadowOffsetPressed");
                    this.Control.Layer.AddAnimation(_shadowRadiusPressed, "shadowRadiusPressed");
                    _animationLayer.AddAnimation(_colorPressed, "backgroundColorPressed");
                });
            }
            else
            {
                await AnimateAsync(0.300, () => _animationLayer.AddAnimation(_colorPressed, "backgroundColorPressed"));
            }
        }

        private async void Control_Released(object sender, EventArgs e)
        {
            var colorAnim = _animationLayer.AnimationForKey("backgroundColorPressed");
            _colorResting.From = colorAnim.ValueForKeyPath(new NSString("backgroundColor"));

            if (_materialButton.ButtonType == MaterialButtonType.Elevated)
            {
                var shadowOffsetAnim = this.Control.Layer.AnimationForKey("shadowOffsetPressed");
                _shadowOffsetResting.From = shadowOffsetAnim.ValueForKeyPath(new NSString("shadowOffset"));

                var shadowRadiusAnim = this.Control.Layer.AnimationForKey("shadowRadiusPressed");
                _shadowRadiusResting.From = shadowRadiusAnim.ValueForKeyPath(new NSString("shadowRadius"));

                await AnimateAsync(0.150, () =>
                {
                    this.Control.Layer.AddAnimation(_shadowOffsetResting, "shadowOffsetResting");
                    this.Control.Layer.AddAnimation(_shadowRadiusResting, "shadowRadiusResting");
                    _animationLayer.AddAnimation(_colorResting, "backgroundColorResting");
                });
            }
            else
            {
                await AnimateAsync(0.300, () => _animationLayer.AddAnimation(_colorResting, "backgroundColorResting"));
            }
        }

        private void CreateContainedButtonLayer(bool elevated)
        {
            _animationLayer.BackgroundColor = _restingBackgroundColor.CGColor;
            _animationLayer.BorderColor = _borderColor.CGColor;
            _animationLayer.BorderWidth = (nfloat)_materialButton.BorderWidth;

            if (elevated)
            {
                this.Control.Elevate(RESTING_ELEVATION);
            }
        }

        private void CreateOutlinedButtonLayer()
        {
            _animationLayer.BorderColor = _materialButton.BorderColor.ToCGColor();
            _animationLayer.BorderWidth = (nfloat)_materialButton.BorderWidth;
        }

        private void CreateStateAnimations()
        {
            if (_materialButton.ButtonType == MaterialButtonType.Elevated)
            {
                _shadowOffsetPressed = CABasicAnimation.FromKeyPath("shadowOffset");
                _shadowOffsetPressed.Duration = 0.150;
                _shadowOffsetPressed.FillMode = CAFillMode.Forwards;
                _shadowOffsetPressed.RemovedOnCompletion = false;
                _shadowOffsetPressed.To = FromObject(new CGSize(0, PRESSED_ELEVATION));

                _shadowRadiusPressed = CABasicAnimation.FromKeyPath("shadowRadius");
                _shadowRadiusPressed.Duration = 0.150;
                _shadowRadiusPressed.FillMode = CAFillMode.Forwards;
                _shadowRadiusPressed.RemovedOnCompletion = false;
                _shadowRadiusPressed.To = NSNumber.FromFloat(PRESSED_ELEVATION);

                _shadowOffsetResting = CABasicAnimation.FromKeyPath("shadowOffset");
                _shadowOffsetResting.Duration = 0.150;
                _shadowOffsetResting.FillMode = CAFillMode.Forwards;
                _shadowOffsetResting.RemovedOnCompletion = false;
                _shadowOffsetResting.To = FromObject(new CGSize(0, RESTING_ELEVATION));

                _shadowRadiusResting = CABasicAnimation.FromKeyPath("shadowRadius");
                _shadowRadiusResting.Duration = 0.150;
                _shadowRadiusResting.FillMode = CAFillMode.Forwards;
                _shadowRadiusResting.RemovedOnCompletion = false;
                _shadowRadiusResting.To = NSNumber.FromFloat(RESTING_ELEVATION);
            }

            _colorPressed = CABasicAnimation.FromKeyPath("backgroundColor");
            _colorPressed.Duration = 0.3;
            _colorPressed.FillMode = CAFillMode.Forwards;
            _colorPressed.RemovedOnCompletion = false;
            _colorPressed.To = FromObject(_pressedBackgroundColor.CGColor);

            _colorResting = CABasicAnimation.FromKeyPath("backgroundColor");
            _colorResting.Duration = 0.3;
            _colorResting.FillMode = CAFillMode.Forwards;
            _colorResting.RemovedOnCompletion = false;
            _colorResting.To = FromObject(_restingBackgroundColor.CGColor);

            this.Control.AddGestureRecognizer(new UITapGestureRecognizer() { Delegate = new MaterialRippleGestureRecognizerDelegate(_rippleColor.CGColor) });
        }

        private void CreateTextButtonLayer()
        {
            _animationLayer.BorderColor = UIColor.Clear.CGColor;
            _animationLayer.BorderWidth = 0f;
        }

        private void SetupColors()
        {
            if (_materialButton.ButtonType == MaterialButtonType.Elevated || _materialButton.ButtonType == MaterialButtonType.Flat)
            {
                _restingBackgroundColor = _materialButton.BackgroundColor.ToUIColor();
                _disabledBackgroundColor = _materialButton.DisabledBackgroundColor.IsDefault ? _materialButton.BackgroundColor.ToUIColor().GetDisabledColor() : _materialButton.DisabledBackgroundColor.ToUIColor();

                if (_materialButton.PressedBackgroundColor.IsDefault)
                {
                    _rippleColor = _materialButton.BackgroundColor.ToUIColor().IsColorDark() ? Color.FromHex("#52FFFFFF").ToUIColor() : Color.FromHex("#52000000").ToUIColor();
                    _pressedBackgroundColor = _restingBackgroundColor.IsColorDark() ? _restingBackgroundColor.LightenColor() : _restingBackgroundColor.DarkenColor();
                }
                else
                {
                    _rippleColor = _materialButton.PressedBackgroundColor.ToUIColor();
                    _pressedBackgroundColor = _restingBackgroundColor.MixColor(_rippleColor);
                }
            }
            else
            {
                _restingBackgroundColor = UIColor.Clear;
                _disabledBackgroundColor = UIColor.Clear;
                _rippleColor = _materialButton.PressedBackgroundColor.IsDefault ? Color.FromHex("#52000000").ToUIColor() : _materialButton.PressedBackgroundColor.ToUIColor();
                _pressedBackgroundColor = _rippleColor;
            }

            _borderColor = _materialButton.ButtonType != MaterialButtonType.Text ? _materialButton.BorderColor.ToUIColor() : UIColor.Clear;
            _disabledBorderColor = _borderColor.GetDisabledColor();
            _normalTextColor = _materialButton.TextColor.ToUIColor();
            _disabledTextColor = _normalTextColor.GetDisabledColor();
        }

        private void SetupIcon()
        {
            if (_withIcon)
            {
                using (var image = UIImage.FromFile(_materialButton.Image.File))
                {
                    UIGraphics.BeginImageContextWithOptions(new CGSize(18, 18), false, 0f);
                    image.Draw(new CGRect(0, 0, 18, 18));

                    using (var newImage = UIGraphics.GetImageFromCurrentImageContext())
                    {
                        UIGraphics.EndImageContext();

                        this.Control.SetImage(newImage, UIControlState.Normal);
                        this.Control.SetImage(newImage, UIControlState.Disabled);
                        this.Control.TitleEdgeInsets = new UIEdgeInsets(0f, 0f, 0f, 0f);
                        this.Control.ImageEdgeInsets = new UIEdgeInsets(0f, -6f, 0f, 0f);
                        this.Control.TintColor = _materialButton.TextColor.ToUIColor();
                    }
                }
            }
        }

        private Task<bool> UpdateBackgroundColor()
        {
            var colorAnim = CABasicAnimation.FromKeyPath("backgroundColor");
            colorAnim.Duration = 0.150;
            colorAnim.FillMode = CAFillMode.Forwards;
            colorAnim.RemovedOnCompletion = false;
            colorAnim.To = FromObject(_restingBackgroundColor.CGColor);

            return AnimateAsync(150, () => _animationLayer.AddAnimation(colorAnim, "colorAnim"));
        }

        private void UpdateButtonLayer()
        {
            if (_animationLayer == null)
            {
                _animationLayer = new CALayer();
                this.Control.Layer.InsertSublayer(_animationLayer, 0);
            }

            switch (_materialButton.ButtonType)
            {
                case MaterialButtonType.Elevated:
                    this.CreateContainedButtonLayer(true);
                    break;

                case MaterialButtonType.Flat:
                    this.CreateContainedButtonLayer(false);
                    break;

                case MaterialButtonType.Outlined:
                    this.CreateOutlinedButtonLayer();
                    break;

                case MaterialButtonType.Text:
                    this.CreateTextButtonLayer();
                    break;
            }

            if (_materialButton.ButtonType != MaterialButtonType.Text && _withIcon)
            {
                this.Control.ContentEdgeInsets = new UIEdgeInsets(10f, 18f, 10f, 22f);
            }
            else if (_materialButton.ButtonType != MaterialButtonType.Text && !_withIcon)
            {
                this.Control.ContentEdgeInsets = new UIEdgeInsets(10f, 22f, 10f, 22f);
            }
            else if (_materialButton.ButtonType == MaterialButtonType.Text)
            {
                this.Control.ContentEdgeInsets = new UIEdgeInsets(10f, 14f, 10f, 14f);
            }
        }

        private void UpdateCornerRadius()
        {
            _animationLayer.CornerRadius = Convert.ToInt32(_materialButton.CornerRadius - _materialButton.CornerRadius * 0.25);
            this.Control.Layer.CornerRadius = _animationLayer.CornerRadius;
        }

        private void UpdateLayerFrame()
        {
            var width = this.Control.Frame.Width - 12;
            var height = this.Control.Frame.Height - 12;

            _animationLayer.Frame = new CGRect(6, 6, width, height);
            _animationLayer.CornerRadius = Convert.ToInt32(_materialButton.CornerRadius - _materialButton.CornerRadius * 0.25);

            this.Control.Layer.BackgroundColor = UIColor.Clear.CGColor;
            this.Control.Layer.BorderColor = UIColor.Clear.CGColor;
            this.Control.Layer.CornerRadius = _animationLayer.CornerRadius;
        }

        private void UpdateState()
        {
            if (_materialButton.IsEnabled)
            {
                _animationLayer.BackgroundColor = _restingBackgroundColor.CGColor;
                _animationLayer.BorderColor = _borderColor.CGColor;

                if (_materialButton.ButtonType == MaterialButtonType.Elevated)
                {
                    this.Control.Elevate(RESTING_ELEVATION);
                }

                if (_materialButton.ButtonType == MaterialButtonType.Elevated || _materialButton.ButtonType == MaterialButtonType.Flat)
                {
                    _materialButton.TextColor = _normalTextColor.ToColor();
                }

                if (_materialButton.ButtonType == MaterialButtonType.Text || _materialButton.ButtonType == MaterialButtonType.Outlined)
                {
                    this.Control.Alpha = 1f;
                }
            }
            else
            {
                _animationLayer.BackgroundColor = _disabledBackgroundColor.CGColor;
                _animationLayer.BorderColor = _disabledBorderColor.CGColor;

                if (_materialButton.ButtonType == MaterialButtonType.Elevated)
                {
                    this.Control.Elevate(0);
                }

                if (_materialButton.ButtonType == MaterialButtonType.Elevated || _materialButton.ButtonType == MaterialButtonType.Flat)
                {
                    _materialButton.TextColor = _disabledTextColor.ToColor();
                }

                if (_materialButton.ButtonType == MaterialButtonType.Text || _materialButton.ButtonType == MaterialButtonType.Outlined)
                {
                    this.Control.Alpha = 0.38f;
                }
            }
        }
    }
}