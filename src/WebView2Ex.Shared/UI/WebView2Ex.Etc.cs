﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.UI.Composition;
using Windows.UI.Xaml;

namespace WebView2Ex.UI;

partial class WebView2Ex
{
    void UpdateSize()
    {
        SetCoreWebViewAndVisualSize((float)ActualWidth, (float)ActualHeight);
    }
    void DisconnectFromRootVisualTarget()
    {
        var CompositionController = this.CompositionController;
        if (CompositionController is not null)
        {
            try
            {
                CompositionController.RootVisualTarget = null;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is COMException)
            {
                // corewebview2 member cannot be accessed after webview2 is disposed
            }
        }
    }

    void SetCoreWebViewAndVisualSize(float width, float height)
    {
        var CoreWebView2 = this.CoreWebView2;
        if (CoreWebView2 == null && visual == null) return;

        if (CoreWebView2 != null)
        {
            CheckAndUpdateWebViewPosition();
        }

        // The CoreWebView2 visuals hosted under the bridge visual are already scaled for the rasterization scale.
        // To keep them from being scaled again from the scale above the WebView2 element, we need to apply
        // an inverse scale on the bridge visual. Since the inverse scale will reduce the size of the bridge visual, we
        // need to scale up the size by the rasterization scale to compensate.

        if (visual != null)
        {
            float m_rasterizationScale = (float)this.rasterizationScale;
            Vector2 newSize = new(width, height);
            Vector2 newSizeScaled = new(width*m_rasterizationScale, height*m_rasterizationScale);
            // targetVisual, source, surface, spriteShapeVisual, roundedRectangle
            (Visuals[0] as Visual).Size = newSize;
            (Visuals[1] as CompositionVisualSurface).SourceSize = newSizeScaled;
            (Visuals[2] as CompositionVisualSurface).SourceSize = newSize;
            (Visuals[3] as Visual).Size = newSize;
            (Visuals[4] as CompositionRoundedRectangleGeometry).Size = newSize;
#if WinUI3
            MUXVisual.Size = newSize;
#endif
        }
    }
}
