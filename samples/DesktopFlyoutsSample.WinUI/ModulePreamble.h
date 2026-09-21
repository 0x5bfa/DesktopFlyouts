#pragma once

#include "pch.h"

#include <unknwn.h>
#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstddef>
#include <cstdint>
#include <functional>
#include <limits>
#include <map>
#include <memory>
#include <mutex>
#include <optional>
#include <regex>
#include <string>
#include <string_view>
#include <type_traits>
#include <utility>
#include <vector>

#include <winrt/base_macros.h>

#define WINRT_IMPORT_MODULE

import winrt_numerics;
import winrt.Windows.Foundation;
import winrt.Windows.Foundation.Collections;
import winrt.Windows.ApplicationModel;
import winrt.Windows.ApplicationModel.Activation;
import winrt.Windows.Globalization.NumberFormatting;
import winrt.Windows.UI;
import winrt.Windows.UI.Xaml;
import winrt.Windows.UI.Xaml.Interop;
import winrt.Microsoft.UI;
import winrt.Microsoft.UI.Composition.SystemBackdrops;
import winrt.Microsoft.UI.Dispatching;
import winrt.Microsoft.UI.Text;
import winrt.Microsoft.UI.Xaml;
import winrt.Microsoft.UI.Xaml.Automation;
import winrt.Microsoft.UI.Xaml.Controls;
import winrt.Microsoft.UI.Xaml.Controls.Primitives;
import winrt.Microsoft.UI.Xaml.Data;
import winrt.Microsoft.UI.Xaml.Interop;
import winrt.Microsoft.UI.Xaml.Markup;
import winrt.Microsoft.UI.Xaml.Media;
import winrt.Microsoft.UI.Xaml.XamlTypeInfo;
import winrt.Microsoft.Windows.ApplicationModel.WindowsAppRuntime;
import winrt.DesktopFlyouts;
import winrt.DesktopFlyoutsSample.WinUI;
