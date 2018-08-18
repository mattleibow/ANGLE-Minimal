using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using EGLSurface = System.IntPtr;

namespace ManagedANGLE
{
    public sealed partial class MainPage : Page
    {
        private readonly object mRenderSurfaceCriticalSection = new object();

        private OpenGLES mOpenGLES;
        private EGLSurface mRenderSurface;
        private IAsyncAction mRenderLoopWorker;

        public MainPage()
        {
            InitializeComponent();

            mOpenGLES = new OpenGLES();
            mRenderSurface = Egl.EGL_NO_SURFACE;

            var window = Window.Current.CoreWindow;
            window.VisibilityChanged += OnVisibilityChanged;

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // The SwapChainPanel has been created and arranged in the page layout,
            // so EGL can be initialized.
            CreateRenderSurface();
            StartRenderLoop();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            StopRenderLoop();
            DestroyRenderSurface();
        }

        private void OnVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
        {
            if (args.Visible && mRenderSurface != Egl.EGL_NO_SURFACE)
            {
                StartRenderLoop();
            }
            else
            {
                StopRenderLoop();
            }
        }

        private void CreateRenderSurface()
        {
            if (mOpenGLES != null && mRenderSurface == Egl.EGL_NO_SURFACE)
            {
                // The app can configure the the SwapChainPanel which may boost performance.
                // By default, this template uses the default configuration.
                mRenderSurface = mOpenGLES.CreateSurface(swapChainPanel);
            }
        }

        private void DestroyRenderSurface()
        {
            mOpenGLES?.DestroySurface(mRenderSurface);
            mRenderSurface = Egl.EGL_NO_SURFACE;
        }

        private void RecoverFromLostDevice()
        {
            // Stop the render loop, reset OpenGLES, recreate the render surface
            // and start the render loop again to recover from a lost device.

            StopRenderLoop();

            lock (mRenderSurfaceCriticalSection)
            {
                DestroyRenderSurface();
                mOpenGLES.Reset();
                CreateRenderSurface();
            }

            StartRenderLoop();
        }

        private void StartRenderLoop()
        {
            // If the render loop is already running then do not start another thread.
            if (mRenderLoopWorker != null && mRenderLoopWorker.Status == AsyncStatus.Started)
            {
                return;
            }

            // Run task on a dedicated high priority background thread.
            mRenderLoopWorker = ThreadPool.RunAsync(workItemHandler, WorkItemPriority.High, WorkItemOptions.TimeSliced);

            // Create a task for rendering that will be run on a background thread.
            void workItemHandler(IAsyncAction action)
            {
                lock (mRenderSurfaceCriticalSection)
                {
                    mOpenGLES.MakeCurrent(mRenderSurface);
                    SimpleRenderer renderer = new SimpleRenderer();

                    while (action.Status == AsyncStatus.Started)
                    {
                        mOpenGLES.GetSurfaceDimensions(mRenderSurface, out var panelWidth, out var panelHeight);

                        // Logic to update the scene could go here
                        renderer.UpdateWindowSize(panelWidth, panelHeight);
                        renderer.Draw();

                        // The call to eglSwapBuffers might not be successful (i.e. due to Device Lost)
                        // If the call fails, then we must reinitialize EGL and the GL resources.
                        if (!mOpenGLES.SwapBuffers(mRenderSurface))
                        {
                            // XAML objects like the SwapChainPanel must only be manipulated on the UI thread.
                            swapChainPanel.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                            {
                                RecoverFromLostDevice();
                            });

                            return;
                        }
                    }
                }
            }
        }

        private void StopRenderLoop()
        {
            mRenderLoopWorker?.Cancel();
            mRenderLoopWorker = null;
        }
    }
}
