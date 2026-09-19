#pragma once

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>
#include <unknwn.h>

#undef GetCurrentTime

#include <algorithm>
#include <cstdint>
#include <mutex>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/author/base.h>
