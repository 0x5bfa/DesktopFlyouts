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
import winrt.Windows.Foundation.Numerics;
import winrt.Windows.UI;
import winrt.Microsoft.UI;
import winrt.Microsoft.UI.Content;
import winrt.Microsoft.UI.Dispatching;
import winrt.Microsoft.UI.Input;
import winrt.Microsoft.UI.Text;
import winrt.Microsoft.UI.Xaml;
import winrt.Microsoft.UI.Xaml.Automation;
import winrt.Microsoft.UI.Xaml.Controls;
import winrt.Microsoft.UI.Xaml.Controls.Primitives;
import winrt.Microsoft.UI.Xaml.Hosting;
import winrt.Microsoft.UI.Xaml.Input;
import winrt.Microsoft.UI.Xaml.Interop;
import winrt.Microsoft.UI.Xaml.Markup;
import winrt.Microsoft.UI.Xaml.Media;
import winrt.Microsoft.UI.Xaml.Media.Animation;
import winrt.DesktopFlyouts;
