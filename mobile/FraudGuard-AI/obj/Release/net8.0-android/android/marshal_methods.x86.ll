; ModuleID = 'marshal_methods.x86.ll'
source_filename = "marshal_methods.x86.ll"
target datalayout = "e-m:e-p:32:32-p270:32:32-p271:32:32-p272:64:64-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i686-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [136 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [272 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 106
	i32 10166715, ; 1: System.Net.NameResolution.dll => 0x9b21bb => 105
	i32 14013194, ; 2: Plugin.AudioRecorder.dll => 0xd5d30a => 48
	i32 39485524, ; 3: System.Net.WebSockets.dll => 0x25a8054 => 113
	i32 42639949, ; 4: System.Threading.Thread => 0x28aa24d => 127
	i32 67008169, ; 5: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 33
	i32 72070932, ; 6: Microsoft.Maui.Graphics.dll => 0x44bb714 => 47
	i32 117431740, ; 7: System.Runtime.InteropServices => 0x6ffddbc => 118
	i32 122350210, ; 8: System.Threading.Channels.dll => 0x74aea82 => 126
	i32 142721839, ; 9: System.Net.WebHeaderCollection => 0x881c32f => 111
	i32 147669188, ; 10: Plugin.Firebase.Core.dll => 0x8cd40c4 => 50
	i32 182336117, ; 11: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 74
	i32 195452805, ; 12: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 30
	i32 199333315, ; 13: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 31
	i32 205061960, ; 14: System.ComponentModel => 0xc38ff48 => 94
	i32 280992041, ; 15: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 2
	i32 317674968, ; 16: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 30
	i32 318968648, ; 17: Xamarin.AndroidX.Activity.dll => 0x13031348 => 51
	i32 336156722, ; 18: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 15
	i32 342366114, ; 19: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 62
	i32 356389973, ; 20: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 14
	i32 367216257, ; 21: Plugin.AudioRecorder => 0x15e34681 => 48
	i32 379916513, ; 22: System.Threading.Thread.dll => 0x16a510e1 => 127
	i32 385762202, ; 23: System.Memory.dll => 0x16fe439a => 102
	i32 395744057, ; 24: _Microsoft.Android.Resource.Designer => 0x17969339 => 34
	i32 435591531, ; 25: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 26
	i32 442565967, ; 26: System.Collections => 0x1a61054f => 91
	i32 450948140, ; 27: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 61
	i32 465846621, ; 28: mscorlib => 0x1bc4415d => 131
	i32 469710990, ; 29: System.dll => 0x1bff388e => 130
	i32 498788369, ; 30: System.ObjectModel => 0x1dbae811 => 115
	i32 500358224, ; 31: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 13
	i32 503918385, ; 32: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 7
	i32 513247710, ; 33: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 42
	i32 539058512, ; 34: Microsoft.Extensions.Logging => 0x20216150 => 39
	i32 592146354, ; 35: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 21
	i32 627609679, ; 36: Xamarin.AndroidX.CustomView => 0x2568904f => 59
	i32 627931235, ; 37: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 19
	i32 662205335, ; 38: System.Text.Encodings.Web.dll => 0x27787397 => 123
	i32 663517072, ; 39: Xamarin.AndroidX.VersionedParcelable => 0x278c7790 => 75
	i32 672442732, ; 40: System.Collections.Concurrent => 0x2814a96c => 88
	i32 683518922, ; 41: System.Net.Security => 0x28bdabca => 109
	i32 688181140, ; 42: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 1
	i32 706645707, ; 43: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 16
	i32 709557578, ; 44: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 4
	i32 722857257, ; 45: System.Runtime.Loader.dll => 0x2b15ed29 => 119
	i32 759454413, ; 46: System.Net.Requests => 0x2d445acd => 108
	i32 775507847, ; 47: System.IO.Compression => 0x2e394f87 => 99
	i32 777317022, ; 48: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 25
	i32 789151979, ; 49: Microsoft.Extensions.Options => 0x2f0980eb => 41
	i32 823281589, ; 50: System.Private.Uri.dll => 0x311247b5 => 116
	i32 830298997, ; 51: System.IO.Compression.Brotli => 0x317d5b75 => 98
	i32 878954865, ; 52: System.Net.Http.Json => 0x3463c971 => 103
	i32 904024072, ; 53: System.ComponentModel.Primitives.dll => 0x35e25008 => 92
	i32 925467409, ; 54: FraudGuardAI.dll => 0x37298311 => 87
	i32 926902833, ; 55: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 28
	i32 965247473, ; 56: Plugin.Firebase.Core => 0x398881f1 => 50
	i32 967690846, ; 57: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 62
	i32 992768348, ; 58: System.Collections.dll => 0x3b2c715c => 91
	i32 1012816738, ; 59: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 72
	i32 1028951442, ; 60: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 38
	i32 1029334545, ; 61: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 3
	i32 1035644815, ; 62: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 52
	i32 1044663988, ; 63: System.Linq.Expressions.dll => 0x3e444eb4 => 100
	i32 1052210849, ; 64: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 64
	i32 1082857460, ; 65: System.ComponentModel.TypeConverter => 0x408b17f4 => 93
	i32 1084122840, ; 66: Xamarin.Kotlin.StdLib => 0x409e66d8 => 85
	i32 1098259244, ; 67: System => 0x41761b2c => 130
	i32 1110581358, ; 68: Xamarin.Firebase.Auth => 0x4232206e => 78
	i32 1118262833, ; 69: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 16
	i32 1168523401, ; 70: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 22
	i32 1178241025, ; 71: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 69
	i32 1203215381, ; 72: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 20
	i32 1234928153, ; 73: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 18
	i32 1246548578, ; 74: Xamarin.AndroidX.Collection.Jvm.dll => 0x4a4cd262 => 55
	i32 1260983243, ; 75: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 2
	i32 1293217323, ; 76: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 60
	i32 1324164729, ; 77: System.Linq => 0x4eed2679 => 101
	i32 1333047053, ; 78: Xamarin.Firebase.Common => 0x4f74af0d => 80
	i32 1373134921, ; 79: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 32
	i32 1376866003, ; 80: Xamarin.AndroidX.SavedState => 0x52114ed3 => 72
	i32 1406073936, ; 81: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 56
	i32 1411702249, ; 82: Xamarin.Firebase.Auth.Interop.dll => 0x5424dde9 => 79
	i32 1430672901, ; 83: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 0
	i32 1452070440, ; 84: System.Formats.Asn1.dll => 0x568cd628 => 97
	i32 1458022317, ; 85: System.Net.Security.dll => 0x56e7a7ad => 109
	i32 1461004990, ; 86: es\Microsoft.Maui.Controls.resources => 0x57152abe => 6
	i32 1462112819, ; 87: System.IO.Compression.dll => 0x57261233 => 99
	i32 1469204771, ; 88: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 53
	i32 1470490898, ; 89: Microsoft.Extensions.Primitives => 0x57a5e912 => 42
	i32 1480492111, ; 90: System.IO.Compression.Brotli.dll => 0x583e844f => 98
	i32 1493001747, ; 91: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 10
	i32 1514721132, ; 92: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 5
	i32 1543031311, ; 93: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 125
	i32 1551623176, ; 94: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 25
	i32 1593325549, ; 95: FraudGuardAI => 0x5ef837ed => 87
	i32 1618516317, ; 96: System.Net.WebSockets.Client.dll => 0x6078995d => 112
	i32 1622152042, ; 97: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 66
	i32 1624863272, ; 98: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 77
	i32 1636350590, ; 99: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 58
	i32 1639515021, ; 100: System.Net.Http.dll => 0x61b9038d => 104
	i32 1639986890, ; 101: System.Text.RegularExpressions => 0x61c036ca => 125
	i32 1657153582, ; 102: System.Runtime => 0x62c6282e => 121
	i32 1658251792, ; 103: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 82
	i32 1677501392, ; 104: System.Net.Primitives.dll => 0x63fca3d0 => 107
	i32 1678508291, ; 105: System.Net.WebSockets => 0x640c0103 => 113
	i32 1679769178, ; 106: System.Security.Cryptography => 0x641f3e5a => 122
	i32 1729485958, ; 107: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 54
	i32 1736233607, ; 108: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 23
	i32 1743415430, ; 109: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 1
	i32 1766324549, ; 110: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 74
	i32 1770582343, ; 111: Microsoft.Extensions.Logging.dll => 0x6988f147 => 39
	i32 1780572499, ; 112: Mono.Android.Runtime.dll => 0x6a216153 => 134
	i32 1782862114, ; 113: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 17
	i32 1788241197, ; 114: Xamarin.AndroidX.Fragment => 0x6a96652d => 61
	i32 1793755602, ; 115: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 9
	i32 1808609942, ; 116: Xamarin.AndroidX.Loader => 0x6bcd3296 => 66
	i32 1813058853, ; 117: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 85
	i32 1813201214, ; 118: Xamarin.Google.Android.Material => 0x6c13413e => 82
	i32 1818569960, ; 119: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 70
	i32 1828688058, ; 120: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 40
	i32 1842015223, ; 121: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 29
	i32 1853025655, ; 122: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 26
	i32 1858542181, ; 123: System.Linq.Expressions => 0x6ec71a65 => 100
	i32 1875053220, ; 124: Xamarin.Firebase.Auth.Interop => 0x6fc30aa4 => 79
	i32 1875935024, ; 125: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 8
	i32 1908813208, ; 126: Xamarin.GooglePlayServices.Basement => 0x71c62d98 => 83
	i32 1910275211, ; 127: System.Collections.NonGeneric.dll => 0x71dc7c8b => 89
	i32 1943407207, ; 128: Plugin.Firebase.Auth => 0x73d60a67 => 49
	i32 1961813231, ; 129: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x74eee4ef => 73
	i32 1968388702, ; 130: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 35
	i32 2003115576, ; 131: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 5
	i32 2019465201, ; 132: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 64
	i32 2025202353, ; 133: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 0
	i32 2045470958, ; 134: System.Private.Xml => 0x79eb68ee => 117
	i32 2055257422, ; 135: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 63
	i32 2066184531, ; 136: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 4
	i32 2079903147, ; 137: System.Runtime.dll => 0x7bf8cdab => 121
	i32 2090596640, ; 138: System.Numerics.Vectors => 0x7c9bf920 => 114
	i32 2127167465, ; 139: System.Console => 0x7ec9ffe9 => 95
	i32 2142473426, ; 140: System.Collections.Specialized => 0x7fb38cd2 => 90
	i32 2159891885, ; 141: Microsoft.Maui => 0x80bd55ad => 45
	i32 2169148018, ; 142: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 12
	i32 2181898931, ; 143: Microsoft.Extensions.Options.dll => 0x820d22b3 => 41
	i32 2192057212, ; 144: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 40
	i32 2193016926, ; 145: System.ObjectModel.dll => 0x82b6c85e => 115
	i32 2201107256, ; 146: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 86
	i32 2201231467, ; 147: System.Net.Http => 0x8334206b => 104
	i32 2207618523, ; 148: it\Microsoft.Maui.Controls.resources => 0x839595db => 14
	i32 2266799131, ; 149: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 36
	i32 2270573516, ; 150: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 8
	i32 2279755925, ; 151: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 71
	i32 2295906218, ; 152: System.Net.Sockets => 0x88d8bfaa => 110
	i32 2303942373, ; 153: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 18
	i32 2305521784, ; 154: System.Private.CoreLib.dll => 0x896b7878 => 132
	i32 2353062107, ; 155: System.Net.Primitives => 0x8c40e0db => 107
	i32 2368005991, ; 156: System.Xml.ReaderWriter.dll => 0x8d24e767 => 129
	i32 2371007202, ; 157: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 35
	i32 2382033717, ; 158: Xamarin.Firebase.Auth.dll => 0x8dfaf335 => 78
	i32 2395872292, ; 159: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 13
	i32 2427813419, ; 160: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 10
	i32 2435356389, ; 161: System.Console.dll => 0x912896e5 => 95
	i32 2458678730, ; 162: System.Net.Sockets.dll => 0x928c75ca => 110
	i32 2475788418, ; 163: Java.Interop.dll => 0x93918882 => 133
	i32 2480646305, ; 164: Microsoft.Maui.Controls => 0x93dba8a1 => 43
	i32 2550873716, ; 165: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 11
	i32 2570120770, ; 166: System.Text.Encodings.Web => 0x9930ee42 => 123
	i32 2593496499, ; 167: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 20
	i32 2605712449, ; 168: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 86
	i32 2617129537, ; 169: System.Private.Xml.dll => 0x9bfe3a41 => 117
	i32 2620871830, ; 170: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 58
	i32 2621637141, ; 171: Plugin.Firebase.Auth.dll => 0x9c430215 => 49
	i32 2626831493, ; 172: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 15
	i32 2663698177, ; 173: System.Runtime.Loader => 0x9ec4cf01 => 119
	i32 2724373263, ; 174: System.Runtime.Numerics.dll => 0xa262a30f => 120
	i32 2732626843, ; 175: Xamarin.AndroidX.Activity => 0xa2e0939b => 51
	i32 2735172069, ; 176: System.Threading.Channels => 0xa30769e5 => 126
	i32 2737747696, ; 177: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 53
	i32 2752995522, ; 178: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 21
	i32 2758225723, ; 179: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 44
	i32 2764765095, ; 180: Microsoft.Maui.dll => 0xa4caf7a7 => 45
	i32 2778768386, ; 181: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 76
	i32 2785988530, ; 182: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 27
	i32 2801831435, ; 183: Microsoft.Maui.Graphics => 0xa7008e0b => 47
	i32 2804607052, ; 184: Xamarin.Firebase.Components.dll => 0xa72ae84c => 81
	i32 2806116107, ; 185: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 6
	i32 2810250172, ; 186: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 56
	i32 2831556043, ; 187: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 19
	i32 2853208004, ; 188: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 76
	i32 2861189240, ; 189: Microsoft.Maui.Essentials => 0xaa8a4878 => 46
	i32 2905242038, ; 190: mscorlib.dll => 0xad2a79b6 => 131
	i32 2909740682, ; 191: System.Private.CoreLib => 0xad6f1e8a => 132
	i32 2916838712, ; 192: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 77
	i32 2919462931, ; 193: System.Numerics.Vectors.dll => 0xae037813 => 114
	i32 2959614098, ; 194: System.ComponentModel.dll => 0xb0682092 => 94
	i32 2978675010, ; 195: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 60
	i32 2987532451, ; 196: Xamarin.AndroidX.Security.SecurityCrypto => 0xb21220a3 => 73
	i32 3038032645, ; 197: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 34
	i32 3057625584, ; 198: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 67
	i32 3058099980, ; 199: Xamarin.GooglePlayServices.Tasks => 0xb646e70c => 84
	i32 3059408633, ; 200: Mono.Android.Runtime => 0xb65adef9 => 134
	i32 3059793426, ; 201: System.ComponentModel.Primitives => 0xb660be12 => 92
	i32 3071899978, ; 202: Xamarin.Firebase.Common.dll => 0xb719794a => 80
	i32 3077302341, ; 203: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 12
	i32 3103600923, ; 204: System.Formats.Asn1 => 0xb8fd311b => 97
	i32 3178803400, ; 205: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 68
	i32 3220365878, ; 206: System.Threading => 0xbff2e236 => 128
	i32 3230466174, ; 207: Xamarin.GooglePlayServices.Basement.dll => 0xc08d007e => 83
	i32 3258312781, ; 208: Xamarin.AndroidX.CardView => 0xc235e84d => 54
	i32 3305363605, ; 209: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 7
	i32 3316684772, ; 210: System.Net.Requests.dll => 0xc5b097e4 => 108
	i32 3317135071, ; 211: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 59
	i32 3346324047, ; 212: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 69
	i32 3357674450, ; 213: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 24
	i32 3358260929, ; 214: System.Text.Json => 0xc82afec1 => 124
	i32 3362522851, ; 215: Xamarin.AndroidX.Core => 0xc86c06e3 => 57
	i32 3366347497, ; 216: Java.Interop => 0xc8a662e9 => 133
	i32 3374999561, ; 217: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 71
	i32 3381016424, ; 218: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 3
	i32 3428513518, ; 219: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 37
	i32 3463511458, ; 220: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 11
	i32 3471940407, ; 221: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 93
	i32 3476120550, ; 222: Mono.Android => 0xcf3163e6 => 135
	i32 3479583265, ; 223: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 24
	i32 3484440000, ; 224: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 23
	i32 3485117614, ; 225: System.Text.Json.dll => 0xcfbaacae => 124
	i32 3580758918, ; 226: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 31
	i32 3598340787, ; 227: System.Net.WebSockets.Client => 0xd67a52b3 => 112
	i32 3608519521, ; 228: System.Linq.dll => 0xd715a361 => 101
	i32 3641597786, ; 229: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 63
	i32 3643446276, ; 230: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 28
	i32 3643854240, ; 231: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 68
	i32 3657292374, ; 232: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 36
	i32 3660523487, ; 233: System.Net.NetworkInformation => 0xda2f27df => 106
	i32 3672681054, ; 234: Mono.Android.dll => 0xdae8aa5e => 135
	i32 3697841164, ; 235: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 33
	i32 3724971120, ; 236: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 67
	i32 3732100267, ; 237: System.Net.NameResolution => 0xde7354ab => 105
	i32 3737834244, ; 238: System.Net.Http.Json.dll => 0xdecad304 => 103
	i32 3748608112, ; 239: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 96
	i32 3792276235, ; 240: System.Collections.NonGeneric => 0xe2098b0b => 89
	i32 3802395368, ; 241: System.Collections.Specialized.dll => 0xe2a3f2e8 => 90
	i32 3823082795, ; 242: System.Security.Cryptography.dll => 0xe3df9d2b => 122
	i32 3841636137, ; 243: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 38
	i32 3849253459, ; 244: System.Runtime.InteropServices.dll => 0xe56ef253 => 118
	i32 3885497537, ; 245: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 111
	i32 3889960447, ; 246: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 32
	i32 3896106733, ; 247: System.Collections.Concurrent.dll => 0xe839deed => 88
	i32 3896760992, ; 248: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 57
	i32 3910130544, ; 249: Xamarin.AndroidX.Collection.Jvm => 0xe90fdb70 => 55
	i32 3921031405, ; 250: Xamarin.AndroidX.VersionedParcelable.dll => 0xe9b630ed => 75
	i32 3928044579, ; 251: System.Xml.ReaderWriter => 0xea213423 => 129
	i32 3931092270, ; 252: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 70
	i32 3955647286, ; 253: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 52
	i32 3970018735, ; 254: Xamarin.GooglePlayServices.Tasks.dll => 0xeca1adaf => 84
	i32 3980434154, ; 255: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 27
	i32 3987592930, ; 256: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 9
	i32 4025784931, ; 257: System.Memory => 0xeff49a63 => 102
	i32 4046471985, ; 258: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 44
	i32 4073602200, ; 259: System.Threading.dll => 0xf2ce3c98 => 128
	i32 4094352644, ; 260: Microsoft.Maui.Essentials.dll => 0xf40add04 => 46
	i32 4100113165, ; 261: System.Private.Uri => 0xf462c30d => 116
	i32 4102112229, ; 262: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 22
	i32 4125707920, ; 263: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 17
	i32 4126470640, ; 264: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 37
	i32 4150914736, ; 265: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 29
	i32 4182413190, ; 266: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 65
	i32 4213026141, ; 267: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 96
	i32 4271975918, ; 268: Microsoft.Maui.Controls.dll => 0xfea12dee => 43
	i32 4274976490, ; 269: System.Runtime.Numerics => 0xfecef6ea => 120
	i32 4284549794, ; 270: Xamarin.Firebase.Components => 0xff610aa2 => 81
	i32 4292120959 ; 271: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 65
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [272 x i32] [
	i32 106, ; 0
	i32 105, ; 1
	i32 48, ; 2
	i32 113, ; 3
	i32 127, ; 4
	i32 33, ; 5
	i32 47, ; 6
	i32 118, ; 7
	i32 126, ; 8
	i32 111, ; 9
	i32 50, ; 10
	i32 74, ; 11
	i32 30, ; 12
	i32 31, ; 13
	i32 94, ; 14
	i32 2, ; 15
	i32 30, ; 16
	i32 51, ; 17
	i32 15, ; 18
	i32 62, ; 19
	i32 14, ; 20
	i32 48, ; 21
	i32 127, ; 22
	i32 102, ; 23
	i32 34, ; 24
	i32 26, ; 25
	i32 91, ; 26
	i32 61, ; 27
	i32 131, ; 28
	i32 130, ; 29
	i32 115, ; 30
	i32 13, ; 31
	i32 7, ; 32
	i32 42, ; 33
	i32 39, ; 34
	i32 21, ; 35
	i32 59, ; 36
	i32 19, ; 37
	i32 123, ; 38
	i32 75, ; 39
	i32 88, ; 40
	i32 109, ; 41
	i32 1, ; 42
	i32 16, ; 43
	i32 4, ; 44
	i32 119, ; 45
	i32 108, ; 46
	i32 99, ; 47
	i32 25, ; 48
	i32 41, ; 49
	i32 116, ; 50
	i32 98, ; 51
	i32 103, ; 52
	i32 92, ; 53
	i32 87, ; 54
	i32 28, ; 55
	i32 50, ; 56
	i32 62, ; 57
	i32 91, ; 58
	i32 72, ; 59
	i32 38, ; 60
	i32 3, ; 61
	i32 52, ; 62
	i32 100, ; 63
	i32 64, ; 64
	i32 93, ; 65
	i32 85, ; 66
	i32 130, ; 67
	i32 78, ; 68
	i32 16, ; 69
	i32 22, ; 70
	i32 69, ; 71
	i32 20, ; 72
	i32 18, ; 73
	i32 55, ; 74
	i32 2, ; 75
	i32 60, ; 76
	i32 101, ; 77
	i32 80, ; 78
	i32 32, ; 79
	i32 72, ; 80
	i32 56, ; 81
	i32 79, ; 82
	i32 0, ; 83
	i32 97, ; 84
	i32 109, ; 85
	i32 6, ; 86
	i32 99, ; 87
	i32 53, ; 88
	i32 42, ; 89
	i32 98, ; 90
	i32 10, ; 91
	i32 5, ; 92
	i32 125, ; 93
	i32 25, ; 94
	i32 87, ; 95
	i32 112, ; 96
	i32 66, ; 97
	i32 77, ; 98
	i32 58, ; 99
	i32 104, ; 100
	i32 125, ; 101
	i32 121, ; 102
	i32 82, ; 103
	i32 107, ; 104
	i32 113, ; 105
	i32 122, ; 106
	i32 54, ; 107
	i32 23, ; 108
	i32 1, ; 109
	i32 74, ; 110
	i32 39, ; 111
	i32 134, ; 112
	i32 17, ; 113
	i32 61, ; 114
	i32 9, ; 115
	i32 66, ; 116
	i32 85, ; 117
	i32 82, ; 118
	i32 70, ; 119
	i32 40, ; 120
	i32 29, ; 121
	i32 26, ; 122
	i32 100, ; 123
	i32 79, ; 124
	i32 8, ; 125
	i32 83, ; 126
	i32 89, ; 127
	i32 49, ; 128
	i32 73, ; 129
	i32 35, ; 130
	i32 5, ; 131
	i32 64, ; 132
	i32 0, ; 133
	i32 117, ; 134
	i32 63, ; 135
	i32 4, ; 136
	i32 121, ; 137
	i32 114, ; 138
	i32 95, ; 139
	i32 90, ; 140
	i32 45, ; 141
	i32 12, ; 142
	i32 41, ; 143
	i32 40, ; 144
	i32 115, ; 145
	i32 86, ; 146
	i32 104, ; 147
	i32 14, ; 148
	i32 36, ; 149
	i32 8, ; 150
	i32 71, ; 151
	i32 110, ; 152
	i32 18, ; 153
	i32 132, ; 154
	i32 107, ; 155
	i32 129, ; 156
	i32 35, ; 157
	i32 78, ; 158
	i32 13, ; 159
	i32 10, ; 160
	i32 95, ; 161
	i32 110, ; 162
	i32 133, ; 163
	i32 43, ; 164
	i32 11, ; 165
	i32 123, ; 166
	i32 20, ; 167
	i32 86, ; 168
	i32 117, ; 169
	i32 58, ; 170
	i32 49, ; 171
	i32 15, ; 172
	i32 119, ; 173
	i32 120, ; 174
	i32 51, ; 175
	i32 126, ; 176
	i32 53, ; 177
	i32 21, ; 178
	i32 44, ; 179
	i32 45, ; 180
	i32 76, ; 181
	i32 27, ; 182
	i32 47, ; 183
	i32 81, ; 184
	i32 6, ; 185
	i32 56, ; 186
	i32 19, ; 187
	i32 76, ; 188
	i32 46, ; 189
	i32 131, ; 190
	i32 132, ; 191
	i32 77, ; 192
	i32 114, ; 193
	i32 94, ; 194
	i32 60, ; 195
	i32 73, ; 196
	i32 34, ; 197
	i32 67, ; 198
	i32 84, ; 199
	i32 134, ; 200
	i32 92, ; 201
	i32 80, ; 202
	i32 12, ; 203
	i32 97, ; 204
	i32 68, ; 205
	i32 128, ; 206
	i32 83, ; 207
	i32 54, ; 208
	i32 7, ; 209
	i32 108, ; 210
	i32 59, ; 211
	i32 69, ; 212
	i32 24, ; 213
	i32 124, ; 214
	i32 57, ; 215
	i32 133, ; 216
	i32 71, ; 217
	i32 3, ; 218
	i32 37, ; 219
	i32 11, ; 220
	i32 93, ; 221
	i32 135, ; 222
	i32 24, ; 223
	i32 23, ; 224
	i32 124, ; 225
	i32 31, ; 226
	i32 112, ; 227
	i32 101, ; 228
	i32 63, ; 229
	i32 28, ; 230
	i32 68, ; 231
	i32 36, ; 232
	i32 106, ; 233
	i32 135, ; 234
	i32 33, ; 235
	i32 67, ; 236
	i32 105, ; 237
	i32 103, ; 238
	i32 96, ; 239
	i32 89, ; 240
	i32 90, ; 241
	i32 122, ; 242
	i32 38, ; 243
	i32 118, ; 244
	i32 111, ; 245
	i32 32, ; 246
	i32 88, ; 247
	i32 57, ; 248
	i32 55, ; 249
	i32 75, ; 250
	i32 129, ; 251
	i32 70, ; 252
	i32 52, ; 253
	i32 84, ; 254
	i32 27, ; 255
	i32 9, ; 256
	i32 102, ; 257
	i32 44, ; 258
	i32 128, ; 259
	i32 46, ; 260
	i32 116, ; 261
	i32 22, ; 262
	i32 17, ; 263
	i32 37, ; 264
	i32 29, ; 265
	i32 65, ; 266
	i32 96, ; 267
	i32 43, ; 268
	i32 120, ; 269
	i32 81, ; 270
	i32 65 ; 271
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

; Functions

; Function attributes: "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" }

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"NumRegisterParameters", i32 0}
