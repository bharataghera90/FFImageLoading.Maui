﻿using System;
using System.IO;
using FFImageLoading.Cache;
using FFImageLoading.Drawables;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using FFImageLoading.DataResolvers;
using System.Runtime.CompilerServices;
using FFImageLoading.Config;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    public class ImageService : ImageServiceBase<SelfDisposingBitmapDrawable>
    {
        readonly Android.Util.DisplayMetrics _metrics = Android.Content.Res.Resources.System.DisplayMetrics;

        static ConditionalWeakTable<object, IImageLoaderTask> _viewsReferences = new ConditionalWeakTable<object, IImageLoaderTask>();
		static IImageService _instance;

		public static IImageService Instance => _instance ??= ServiceHelper.GetService<IImageService>();

		public ImageService(
			IConfiguration configuration,
			IMD5Helper md5Helper,
			IMiniLogger miniLogger,
			IPlatformPerformance platformPerformance,
			IMainThreadDispatcher mainThreadDispatcher,
			IDataResolverFactory dataResolverFactory,
			IDiskCache diskCache,
			IDownloadCache downloadCache,
			IWorkScheduler workScheduler)
			: base(configuration, md5Helper, miniLogger, platformPerformance, mainThreadDispatcher, dataResolverFactory, diskCache, downloadCache, workScheduler)
		{
		}

		public override IMemoryCache<SelfDisposingBitmapDrawable> MemoryCache => ImageCache.Instance;

		public static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<SelfDisposingBitmapDrawable, TImageView> target) where TImageView : class
		{
			return new PlatformImageLoaderTask<TImageView>(Instance, target, parameters);
		}

		public override IImageLoaderTask CreateTask(TaskParameter parameters)
		{
			return new PlatformImageLoaderTask<object>(this, null, parameters);
		}

		protected override void SetTaskForTarget(IImageLoaderTask currentTask)
        {
            var targetView = currentTask?.Target?.TargetControl;

            if (!(targetView is Android.Views.View))
                return;

            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(targetView, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }

                    _viewsReferences.Remove(targetView);
                }

                _viewsReferences.Add(targetView, currentTask);
            }
        }

        public override void CancelWorkForView(object view)
        {
            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(view, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        public override int DpToPixels(double dp, double scale)
        {
			// double px = dp * ((float)_metrics.DensityDpi / 160f);
			double px = dp * scale;
			return (int)Math.Floor(px);
        }

        public override double PixelsToDp(double px, double scale)
        {
            if (Math.Abs(px) < double.Epsilon)
                return 0;

			//return px / ((float)_metrics.DensityDpi / 160f);
			return px / scale;
		}
    }
}
