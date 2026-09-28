#if HAS_UNO
using Tmds.DBus.Protocol;
using DesktopFlyouts.DBus;
using SkiaSharp;
using Svg.Skia;

namespace DesktopFlyouts;

/// <summary>
/// Provides functionality for displaying and managing a system tray icon on Linux via D-Bus StatusNotifierItem.
/// </summary>
/// <remarks>
/// <see cref="SystemTrayIcon"/> wraps the StatusNotifierItem D-Bus protocol and raises click events
/// with screen coordinates that can be passed to the point-based flyout and menu show methods.
/// Call <see cref="Show"/> to register the icon with the StatusNotifierWatcher and
/// <see cref="Destroy"/> to remove it. Call <see cref="Dispose()"/> explicitly when the object
/// is no longer needed to release its owned resources.
/// </remarks>
public class SystemTrayIcon : IDisposable
{
    readonly string _id;
    readonly object _lifecycleLock = new();
    readonly SynchronizationContext? _eventContext;
    DBusConnection? _connection;
    DBus.DBus? _dBus;
    StatusNotifierWatcher? _statusNotifierWatcher;
    X11StatusNotifierItemHandler? _sniHandler;
    IDisposable? _serviceWatchDisposable;
    CancellationTokenSource? _connectionCancellation;
    int _connectionGeneration;
    bool _isDisposed;
    bool _serviceConnected;
    bool _isRegistered;
    bool _registrationPending;
    bool _isVisible;

    (int, int, byte[]) _currentIcon = (1, 1, new byte[] { 255, 0, 0, 0 });

    const int GnomeShellInitialDelayMs = 100;
    const int GnomeShellSecondDelayMs = 400;

    string _tooltip;
    string _iconPath;

    public SystemTrayIcon(string iconPath, string tooltip, System.Guid id)
        : this(iconPath, tooltip, id.ToString()) { }

    /// <summary>
    /// Initializes a new instance of <see cref="SystemTrayIcon"/> with an icon loaded from a
    /// file path.
    /// </summary>
    /// <param name="iconPath">The path to the icon file.</param>
    /// <param name="tooltip">The tooltip text.</param>
    /// <param name="id">The stable identifier for the tray icon.</param>
    /// <remarks>
    /// Construction prepares the icon resources. D-Bus initialization and registration
    /// begin when <see cref="Show"/> is called.
    /// </remarks>
    public SystemTrayIcon(string iconPath, string tooltip, string id)
    {
        ThrowHelper.ThrowIfNotLinux();
        _id = id;
        _iconPath = iconPath;
        _tooltip = tooltip;
        _eventContext = SynchronizationContext.Current;

        _currentIcon = RenderIcon(iconPath);
    }

    void OnActivation(int x, int y)
    {
        RaiseOnCapturedContext(() =>
            LeftClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y))));
    }

    void OnContextMenu(int x, int y)
    {
        RaiseOnCapturedContext(() =>
            RightClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y))));
    }

    void OnSecondaryActivate(int x, int y)
    {
        RaiseOnCapturedContext(() =>
            MiddleClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y))));
    }

    void OnScroll(int delta, string orientation)
    {
        RaiseOnCapturedContext(() =>
            Scrolled?.Invoke(this, new MouseScrollEventReceivedEventArgs(
                delta,
                orientation == "horizontal"
                    ? MouseScrollOrientation.Horizontal
                    : MouseScrollOrientation.Vertical)));
    }

    // ─── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the tooltip text shown for the tray icon.
    /// </summary>
    /// <value>The tray icon tooltip text.</value>
    /// <remarks>
    /// If the tray icon is already registered with the StatusNotifierWatcher, setting this property
    /// updates it immediately. Otherwise, the value is recorded and applied when <see cref="Show"/> is called.
    /// </remarks>
    public string Tooltip
    {
        get
        {
            lock (_lifecycleLock)
                return _tooltip;
        }
        set
        {
            X11StatusNotifierItemHandler? handler;
            lock (_lifecycleLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(SystemTrayIcon));

                _tooltip = value;
                handler = _isRegistered ? _sniHandler : null;
            }

            handler?.SetTitleAndTooltip(value);
        }
    }

    /// <summary>
    /// Gets the full path to the current icon file.
    /// </summary>
    /// <value>The full path to an icon file.</value>
    public string IconPath => _iconPath;

    /// <summary>
    /// Replaces the current icon with one loaded from a file path.
    /// </summary>
    /// <param name="iconPath">The path to the icon file.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="iconPath"/> cannot be found or loaded as an icon file.
    /// </exception>
    public void SetIcon(string iconPath)
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SystemTrayIcon));
        }

        var icon = RenderIcon(iconPath);
        X11StatusNotifierItemHandler? handler;
        lock (_lifecycleLock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SystemTrayIcon));

            _iconPath = iconPath;
            _currentIcon = icon;
            handler = _isRegistered ? _sniHandler : null;
        }

        handler?.SetIcon(icon);
    }

    /// <summary>
    /// Makes the tray icon visible and synchronizes its state to the StatusNotifierWatcher.
    /// </summary>
    /// <remarks>
    /// If the tray icon has not yet been registered, this method registers it. If it
    /// already exists, this method updates its current state.
    /// </remarks>
    public void Show()
    {
        int generation;
        bool initialize;
        bool register;
        CancellationToken initializationToken = default;
        lock (_lifecycleLock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SystemTrayIcon));

            _isVisible = true;
            initialize = _connection is null && _connectionCancellation is null;
            if (initialize)
            {
                _connectionCancellation = new CancellationTokenSource();
                generation = ++_connectionGeneration;
                initializationToken = _connectionCancellation.Token;
            }
            else
            {
                generation = _connectionGeneration;
            }

            register = _serviceConnected && !_isRegistered && !_registrationPending;
        }

        if (initialize)
            _ = InitializeAsync(generation, initializationToken);
        else if (register)
            _ = CreateTrayIconAsync(generation);
    }

    /// <summary>
    /// Removes the tray icon from the StatusNotifierWatcher.
    /// </summary>
    /// <remarks>
    /// This method is safe to call multiple times. It does not dispose the
    /// <see cref="SystemTrayIcon"/> object; call <see cref="Dispose()"/> to release all resources.
    /// </remarks>
    public void Destroy()
    {
        CloseConnection(disposeObject: false);
    }

    /// <summary>
    /// Releases the resources held by this <see cref="SystemTrayIcon"/>.
    /// </summary>
    /// <remarks>
    /// Call this method explicitly when the instance is no longer needed. If the tray icon is
    /// still registered, it is removed first. The D-Bus connection and associated watches are
    /// disposed. After disposal, the instance should not be used again.
    /// </remarks>
    public void Dispose()
    {
        CloseConnection(disposeObject: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets or sets whether the tray icon is visible.
    /// </summary>
    /// <value><see langword="true"/> if the tray icon is visible; otherwise,
    /// <see langword="false"/>. The default is <see langword="false"/>.</value>
    /// <remarks>
    /// If the tray icon is already registered with the StatusNotifierWatcher, setting this property
    /// updates the icon state immediately. Otherwise, the value is recorded and applied when
    /// <see cref="Show"/> is called.
    /// </remarks>
    public bool IsVisible
    {
        get
        {
            lock (_lifecycleLock)
                return _isVisible;
        }
        set
        {
            if (value) Show();
            else Destroy();
        }
    }

    /// <summary>
    /// Gets the stable identifier used for the tray icon.
    /// </summary>
    /// <value>The identifier used by the StatusNotifierWatcher to identify this notification icon.</value>
    public string Id => _id;

    /// <summary>
    /// Occurs when the tray icon receives a left-click (activation).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? LeftClicked;

    /// <summary>
    /// Occurs when the tray icon receives a right-click (context menu).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? RightClicked;

    /// <summary>
    /// Occurs when the tray icon receives a middle-click (secondary activation).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? MiddleClicked;

    /// <summary>
    /// Occurs when the tray icon receives a scroll event.
    /// </summary>
    /// <remarks>
    /// The event argument contains the scroll delta and orientation.
    /// </remarks>
    public event EventHandler<MouseScrollEventReceivedEventArgs>? Scrolled;
    // Linux does not have this
    // public event EventHandler<MouseEventReceivedEventArgs>? LeftDoubleClicked;
    // public event EventHandler<MouseEventReceivedEventArgs>? RightDoubleClicked;

    // ─── D-Bus Initialization ─────────────────────────────────────

    async Task InitializeAsync(int generation, CancellationToken cancellationToken)
    {
        DBusConnection? connection = null;
        X11StatusNotifierItemHandler? handler = null;
        var attached = false;
        try
        {
            var sessionAddress = DBusAddress.Session;
            if (sessionAddress is null)
                return;

            connection = new DBusConnection(sessionAddress);
            await connection.ConnectAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var dBus = new DBus.DBus(connection, "org.freedesktop.DBus", "/org/freedesktop/DBus");
            handler = new X11StatusNotifierItemHandler(connection, _id, _id);
            handler.ActivationDelegate += OnActivation;
            handler.ContextMenuDelegate += OnContextMenu;
            handler.SecondaryActivateDelegate += OnSecondaryActivate;
            handler.ScrollDelegate += OnScroll;
            connection.AddMethodHandler(handler);

            lock (_lifecycleLock)
            {
                if (_isDisposed || !_isVisible || generation != _connectionGeneration)
                    return;

                _connection = connection;
                _dBus = dBus;
                _sniHandler = handler;
                attached = true;
            }

            connection = null;
            handler = null;
            await WatchAsync(generation, dBus, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            if (!attached)
            {
                DetachHandler(handler);
                try
                {
                    if (connection is not null && handler is not null)
                        connection.RemoveMethodHandler(handler.Path);
                }
                catch
                {
                }

                connection?.Dispose();
                lock (_lifecycleLock)
                {
                    if (generation == _connectionGeneration && _connection is null)
                    {
                        _connectionCancellation?.Dispose();
                        _connectionCancellation = null;
                    }
                }
            }
        }
    }

    async Task WatchAsync(
        int generation,
        DBus.DBus dBus,
        CancellationToken cancellationToken)
    {
        IDisposable? watch = null;
        try
        {
            watch = await dBus.WatchNameOwnerChangedAsync(
                change =>
                {
                    if (change.A0 == "org.kde.StatusNotifierWatcher")
                        OnNameChange(generation, change.A0, change.A2);
                },
                emitOnCapturedContext: false);
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lifecycleLock)
            {
                if (_isDisposed || generation != _connectionGeneration)
                    return;

                _serviceWatchDisposable = watch;
                watch = null;
            }

            try
            {
                var nameOwner = await dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
                OnNameChange(generation, "org.kde.StatusNotifierWatcher", nameOwner);
            }
            catch (DBusErrorReplyException ex) when (
                ex.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
            {
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            watch?.Dispose();
        }
    }

    void OnNameChange(int generation, string name, string? newOwner)
    {
        var register = false;
        lock (_lifecycleLock)
        {
            if (_isDisposed || generation != _connectionGeneration || _connection is null ||
                name != "org.kde.StatusNotifierWatcher")
                return;

            if (!string.IsNullOrEmpty(newOwner))
            {
                _serviceConnected = true;
                _isRegistered = false;
                _registrationPending = false;
                _statusNotifierWatcher = new StatusNotifierWatcher(
                    _connection,
                    "org.kde.StatusNotifierWatcher",
                    "/StatusNotifierWatcher");
                register = _isVisible;
            }
            else
            {
                _serviceConnected = false;
                _isRegistered = false;
                _registrationPending = false;
                _statusNotifierWatcher = null;
            }
        }

        if (register)
            _ = CreateTrayIconAsync(generation);
    }

    async Task CreateTrayIconAsync(int generation)
    {
        DBusConnection connection;
        StatusNotifierWatcher watcher;
        X11StatusNotifierItemHandler handler;
        lock (_lifecycleLock)
        {
            if (_isDisposed || !_isVisible || !_serviceConnected || _isRegistered ||
                _registrationPending ||
                generation != _connectionGeneration || _connection is null ||
                _statusNotifierWatcher is null || _sniHandler is null)
                return;

            _registrationPending = true;
            connection = _connection;
            watcher = _statusNotifierWatcher;
            handler = _sniHandler;
        }

        try
        {
            await watcher.RegisterStatusNotifierItemAsync(connection.UniqueName!);

            string tooltip;
            (int, int, byte[]) icon;
            CancellationToken cancellationToken;
            lock (_lifecycleLock)
            {
                if (_isDisposed || !_isVisible || !_serviceConnected ||
                    generation != _connectionGeneration ||
                    !ReferenceEquals(connection, _connection) ||
                    !ReferenceEquals(watcher, _statusNotifierWatcher))
                    return;

                _registrationPending = false;
                _isRegistered = true;
                tooltip = _tooltip;
                icon = _currentIcon;
                cancellationToken = _connectionCancellation?.Token ?? default;
            }

            handler.SetTitleAndTooltip(tooltip);
            handler.SetIcon(icon);
            _ = ReEmitSignalsForGnomeShellAsync(generation, cancellationToken);
        }
        catch
        {
            lock (_lifecycleLock)
            {
                if (generation == _connectionGeneration)
                    _registrationPending = false;
            }
        }
    }

    async Task ReEmitSignalsForGnomeShellAsync(
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(GnomeShellInitialDelayMs, cancellationToken);
            ReEmitIconIfCurrent(generation);

            await Task.Delay(GnomeShellSecondDelayMs, cancellationToken);
            ReEmitIconIfCurrent(generation);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    void ReEmitIconIfCurrent(int generation)
    {
        X11StatusNotifierItemHandler? handler;
        (int, int, byte[]) icon;
        lock (_lifecycleLock)
        {
            if (_isDisposed || !_isVisible || !_isRegistered ||
                generation != _connectionGeneration)
                return;

            handler = _sniHandler;
            icon = _currentIcon;
        }

        handler?.SetIcon(icon);
    }

    void CloseConnection(bool disposeObject)
    {
        CancellationTokenSource? cancellation;
        IDisposable? watch;
        DBusConnection? connection;
        X11StatusNotifierItemHandler? handler;
        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                if (disposeObject)
                    return;
                throw new ObjectDisposedException(nameof(SystemTrayIcon));
            }

            if (disposeObject)
                _isDisposed = true;

            _isVisible = false;
            _connectionGeneration++;
            cancellation = _connectionCancellation;
            watch = _serviceWatchDisposable;
            connection = _connection;
            handler = _sniHandler;
            _connectionCancellation = null;
            _serviceWatchDisposable = null;
            _connection = null;
            _dBus = null;
            _statusNotifierWatcher = null;
            _sniHandler = null;
            _serviceConnected = false;
            _isRegistered = false;
            _registrationPending = false;
        }

        try
        {
            cancellation?.Cancel();
        }
        catch
        {
        }
        cancellation?.Dispose();

        DetachHandler(handler);
        try
        {
            if (connection is not null && handler is not null)
                connection.RemoveMethodHandler(handler.Path);
        }
        catch
        {
        }

        try
        {
            watch?.Dispose();
        }
        catch
        {
        }

        try
        {
            // Closing the unique D-Bus name is the StatusNotifierItem protocol's
            // unregister operation. The watcher removes the item automatically.
            connection?.Dispose();
        }
        catch
        {
        }
    }

    void DetachHandler(X11StatusNotifierItemHandler? handler)
    {
        if (handler is null)
            return;

        handler.ActivationDelegate -= OnActivation;
        handler.ContextMenuDelegate -= OnContextMenu;
        handler.SecondaryActivateDelegate -= OnSecondaryActivate;
        handler.ScrollDelegate -= OnScroll;
    }

    void RaiseOnCapturedContext(Action action)
    {
        if (_eventContext is null || ReferenceEquals(SynchronizationContext.Current, _eventContext))
        {
            bool invoke;
            lock (_lifecycleLock)
                invoke = !_isDisposed;
            if (invoke)
                action();
        }
        else
        {
            _eventContext.Post(_ =>
            {
                bool invoke;
                lock (_lifecycleLock)
                    invoke = !_isDisposed;
                if (invoke)
                    action();
            }, null);
        }
    }

    // ─── Icon Rendering ───────────────────────────────────────────

    static (int width, int height, byte[] argbData) RenderIcon(string path, int size = 48)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Icon file not found: {path}");

        try
        {
            using var svg = new SKSvg();
            if (svg.Load(path) is { } picture)
            {
                using var bitmap = RenderSvgToBitmap(picture, size);
                return ConvertToArgb(bitmap);
            }
        }
        catch
        {
        }

        using var src = SKBitmap.Decode(path)
            ?? throw new InvalidOperationException($"Failed to decode image: {path}");

        using var scaled = ScaleBitmap(src, size);
        return ConvertToArgb(scaled);
    }

    static SKBitmap RenderSvgToBitmap(SKPicture picture, int size)
    {
        var bounds = picture.CullRect;
        var scale = Math.Min(size / bounds.Width, size / bounds.Height);
        var width = Math.Max(1, (int)(bounds.Width * scale));
        var height = Math.Max(1, (int)(bounds.Height * scale));

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        return bitmap;
    }

    static SKBitmap ScaleBitmap(SKBitmap source, int size)
    {
        var scale = Math.Min((float)size / source.Width, (float)size / source.Height);
        var width = Math.Max(1, (int)(source.Width * scale));
        var height = Math.Max(1, (int)(source.Height * scale));

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height));
        return bitmap;
    }

    static (int width, int height, byte[] argbData) ConvertToArgb(SKBitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var pixels = bitmap.Bytes;
        var argbData = new byte[width * height * 4];

        for (int i = 0; i < width * height; i++)
        {
            var srcIdx = i * 4;
            argbData[srcIdx] = pixels[srcIdx + 3];
            argbData[srcIdx + 1] = pixels[srcIdx];
            argbData[srcIdx + 2] = pixels[srcIdx + 1];
            argbData[srcIdx + 3] = pixels[srcIdx + 2];
        }

        return (width, height, argbData);
    }
}
#endif
