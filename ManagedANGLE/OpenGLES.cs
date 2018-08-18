using System;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;

using EGLConfig = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLDisplay = System.IntPtr;
using EGLint = System.Int32;
using EGLSurface = System.IntPtr;

namespace ManagedANGLE
{
    public class OpenGLES : IDisposable
    {
        private EGLDisplay mEglDisplay;
        private EGLContext mEglContext;
        private EGLConfig mEglConfig;

        public OpenGLES()
        {
            mEglConfig = Egl.EGL_NO_CONFIG;
            mEglDisplay = Egl.EGL_NO_DISPLAY;
            mEglContext = Egl.EGL_NO_CONTEXT;

            Initialize();
        }

        public void Dispose()
        {
            Cleanup();
        }

        public EGLSurface CreateSurface(SwapChainPanel panel)
        {
            if (panel == null)
            {
                throw new ArgumentNullException("SwapChainPanel parameter is invalid");
            }

            EGLSurface surface = Egl.EGL_NO_SURFACE;

            EGLint[] surfaceAttributes = new[]
            {
                // EGL_ANGLE_SURFACE_RENDER_TO_BACK_BUFFER is part of the same optimization as EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER (see above).
                // If you have compilation issues with it then please update your Visual Studio templates.
                Egl.EGL_ANGLE_SURFACE_RENDER_TO_BACK_BUFFER, Egl.EGL_TRUE,
                Egl.EGL_NONE
            };

            // Create a PropertySet and initialize with the EGLNativeWindowType.
            PropertySet surfaceCreationProperties = new PropertySet
            {
                { Egl.EGLNativeWindowTypeProperty, panel }
            };

            surface = Egl.eglCreateWindowSurface(mEglDisplay, mEglConfig, surfaceCreationProperties, surfaceAttributes);
            if (surface == Egl.EGL_NO_SURFACE)
            {
                throw new Exception("Failed to create EGL surface");
            }

            return surface;
        }

        public void GetSurfaceDimensions(EGLSurface surface, out EGLint width, out EGLint height)
        {
            Egl.eglQuerySurface(mEglDisplay, surface, Egl.EGL_WIDTH, out width);
            Egl.eglQuerySurface(mEglDisplay, surface, Egl.EGL_HEIGHT, out height);
        }

        public void DestroySurface(EGLSurface surface)
        {
            if (mEglDisplay != Egl.EGL_NO_DISPLAY && surface != Egl.EGL_NO_SURFACE)
            {
                Egl.eglDestroySurface(mEglDisplay, surface);
            }
        }

        public void MakeCurrent(EGLSurface surface)
        {
            if (Egl.eglMakeCurrent(mEglDisplay, surface, surface, mEglContext) == Egl.EGL_FALSE)
            {
                throw new Exception("Failed to make EGLSurface current");
            }
        }

        public bool SwapBuffers(EGLSurface surface)
        {
            return (Egl.eglSwapBuffers(mEglDisplay, surface) == Egl.EGL_TRUE);
        }

        public void Reset()
        {
            Cleanup();
            Initialize();
        }

        private void Initialize()
        {
            EGLint[] configAttributes = new[]
            {
                Egl.EGL_RED_SIZE, 8,
                Egl.EGL_GREEN_SIZE, 8,
                Egl.EGL_BLUE_SIZE, 8,
                Egl.EGL_ALPHA_SIZE, 8,
                Egl.EGL_DEPTH_SIZE, 8,
                Egl.EGL_STENCIL_SIZE, 8,
                Egl.EGL_NONE
            };

            EGLint[] contextAttributes = new[]
            {
                Egl.EGL_CONTEXT_CLIENT_VERSION, 2,
                Egl.EGL_NONE
            };

            EGLint[] defaultDisplayAttributes = new[]
            {
                // These are the default display attributes, used to request ANGLE's D3D11 renderer.
                // eglInitialize will only succeed with these attributes if the hardware supports D3D11 Feature Level 10_0+.
                Egl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, Egl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,

                // EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER is an optimization that can have large performance benefits on mobile devices.
                // Its syntax is subject to change, though. Please update your Visual Studio templates if you experience compilation issues with it.
                Egl.EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER, Egl.EGL_TRUE, 

                // EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE is an option that enables ANGLE to automatically call 
                // the IDXGIDevice3::Trim method on behalf of the application when it gets suspended. 
                // Calling IDXGIDevice3::Trim when an application is suspended is a Windows Store application certification requirement.
                Egl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, Egl.EGL_TRUE,
                Egl.EGL_NONE,
            };

            EGLint[] fl9_3DisplayAttributes = new[]
            {
                // These can be used to request ANGLE's D3D11 renderer, with D3D11 Feature Level 9_3.
                // These attributes are used if the call to eglInitialize fails with the default display attributes.
                Egl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, Egl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,
                Egl.EGL_PLATFORM_ANGLE_MAX_VERSION_MAJOR_ANGLE, 9,
                Egl.EGL_PLATFORM_ANGLE_MAX_VERSION_MINOR_ANGLE, 3,
                Egl.EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER, Egl.EGL_TRUE,
                Egl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, Egl.EGL_TRUE,
                Egl.EGL_NONE,
            };

            EGLint[] warpDisplayAttributes = new[]
            {
                // These attributes can be used to request D3D11 WARP.
                // They are used if eglInitialize fails with both the default display attributes and the 9_3 display attributes.
                Egl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, Egl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,
                Egl.EGL_PLATFORM_ANGLE_DEVICE_TYPE_ANGLE, Egl.EGL_PLATFORM_ANGLE_DEVICE_TYPE_WARP_ANGLE,
                Egl.EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER, Egl.EGL_TRUE,
                Egl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, Egl.EGL_TRUE,
                Egl.EGL_NONE,
            };

            EGLConfig config = IntPtr.Zero;

            //
            // To initialize the display, we make three sets of calls to eglGetPlatformDisplayEXT and eglInitialize, with varying 
            // parameters passed to eglGetPlatformDisplayEXT:
            // 1) The first calls uses "defaultDisplayAttributes" as a parameter. This corresponds to D3D11 Feature Level 10_0+.
            // 2) If eglInitialize fails for step 1 (e.g. because 10_0+ isn't supported by the default GPU), then we try again 
            //    using "fl9_3DisplayAttributes". This corresponds to D3D11 Feature Level 9_3.
            // 3) If eglInitialize fails for step 2 (e.g. because 9_3+ isn't supported by the default GPU), then we try again 
            //    using "warpDisplayAttributes".  This corresponds to D3D11 Feature Level 11_0 on WARP, a D3D11 software rasterizer.
            //

            // This tries to initialize EGL to D3D11 Feature Level 10_0+. See above comment for details.
            mEglDisplay = Egl.eglGetPlatformDisplayEXT(Egl.EGL_PLATFORM_ANGLE_ANGLE, Egl.EGL_DEFAULT_DISPLAY, defaultDisplayAttributes);
            if (mEglDisplay == Egl.EGL_NO_DISPLAY)
            {
                throw new Exception("Failed to get EGL display");
            }

            if (Egl.eglInitialize(mEglDisplay, out EGLint major, out EGLint minor) == Egl.EGL_FALSE)
            {
                // This tries to initialize EGL to D3D11 Feature Level 9_3, if 10_0+ is unavailable (e.g. on some mobile devices).
                mEglDisplay = Egl.eglGetPlatformDisplayEXT(Egl.EGL_PLATFORM_ANGLE_ANGLE, Egl.EGL_DEFAULT_DISPLAY, fl9_3DisplayAttributes);
                if (mEglDisplay == Egl.EGL_NO_DISPLAY)
                {
                    throw new Exception("Failed to get EGL display");
                }

                if (Egl.eglInitialize(mEglDisplay, out major, out minor) == Egl.EGL_FALSE)
                {
                    // This initializes EGL to D3D11 Feature Level 11_0 on WARP, if 9_3+ is unavailable on the default GPU.
                    mEglDisplay = Egl.eglGetPlatformDisplayEXT(Egl.EGL_PLATFORM_ANGLE_ANGLE, Egl.EGL_DEFAULT_DISPLAY, warpDisplayAttributes);
                    if (mEglDisplay == Egl.EGL_NO_DISPLAY)
                    {
                        throw new Exception("Failed to get EGL display");
                    }

                    if (Egl.eglInitialize(mEglDisplay, out major, out minor) == Egl.EGL_FALSE)
                    {
                        // If all of the calls to eglInitialize returned EGL_FALSE then an error has occurred.
                        throw new Exception("Failed to initialize EGL");
                    }
                }
            }

            EGLDisplay[] configs = new EGLDisplay[1];
            if ((Egl.eglChooseConfig(mEglDisplay, configAttributes, configs, configs.Length, out EGLint numConfigs) == Egl.EGL_FALSE) || (numConfigs == 0))
            {
                throw new Exception("Failed to choose first EGLConfig");
            }
            mEglConfig = configs[0];

            mEglContext = Egl.eglCreateContext(mEglDisplay, mEglConfig, Egl.EGL_NO_CONTEXT, contextAttributes);
            if (mEglContext == Egl.EGL_NO_CONTEXT)
            {
                throw new Exception("Failed to create EGL context");
            }
        }

        private void Cleanup()
        {
            if (mEglDisplay != Egl.EGL_NO_DISPLAY && mEglContext != Egl.EGL_NO_CONTEXT)
            {
                Egl.eglDestroyContext(mEglDisplay, mEglContext);
                mEglContext = Egl.EGL_NO_CONTEXT;
            }

            if (mEglDisplay != Egl.EGL_NO_DISPLAY)
            {
                Egl.eglTerminate(mEglDisplay);
                mEglDisplay = Egl.EGL_NO_DISPLAY;
            }
        }
    }
}
