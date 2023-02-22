using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Extensions;

namespace OwlCore.Storage.CommonTests
{
    public abstract class CommonIFileTests
    {
        /// <summary>
        /// Gets a boolean that indicates if the file support writing to the underlying stream.
        /// </summary>
        public virtual bool SupportsWriting => true;

        /// <summary>
        /// Call the constructor using valid input parameters.
        /// </summary>
        public abstract Task<IFile> CreateFileAsync();

        [TestMethod]
        public Task ConstructorCall_ValidParameters()
        {
            // Shouldn't throw when constructor is called.
            return CreateFileAsync();
        }

        [TestMethod]
        public async Task IdNotNullOrWhiteSpace()
        {
            var file = await CreateFileAsync();

            Assert.IsFalse(string.IsNullOrWhiteSpace(file.Id));
        }

        [TestMethod]
        [AllEnumFlagCombinations(typeof(FileAccess))]
        public async Task OpenStreamAndTryEachAccessMode(FileAccess accessMode)
        {
            var file = await CreateFileAsync();

            if (accessMode == 0)
            {
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode));
                return;
            }

            // Don't test writing if not supported.
            if (!SupportsWriting)
                accessMode ^= FileAccess.Write;

            // If removing write access resulted in empty flag.
            if (accessMode == 0)
                return;

            using var stream = await file.OpenStreamAsync(accessMode);

            if (accessMode.HasFlag(FileAccess.Read))
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                Assert.AreNotEqual(0, memoryStream.ToArray().Length);
            }

            if (accessMode.HasFlag(FileAccess.Write) && SupportsWriting)
            {
                stream.WriteByte(0);
            }
        }

        [TestMethod]
        [AllEnumFlagCombinations(typeof(FileAccess))]
        public async Task OpenStreamWithEachAccessModeAndCancelToken(FileAccess accessMode)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var file = await CreateFileAsync();

            if (accessMode == 0)
            {
                var task = Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => file.OpenStreamAsync(accessMode, cancellationTokenSource.Token));
                cancellationTokenSource.Cancel();

                await task;
                return;
            }

            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => file.OpenStreamAsync(accessMode, cancellationTokenSource.Token));
        }
    }

}