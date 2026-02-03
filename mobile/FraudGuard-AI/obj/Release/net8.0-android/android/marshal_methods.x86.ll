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

@assembly_image_cache = dso_local local_unnamed_addr global [126 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [252 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 96
	i32 10166715, ; 1: System.Net.NameResolution.dll => 0x9b21bb => 95
	i32 14013194, ; 2: Plugin.AudioRecorder.dll => 0xd5d30a => 48
	i32 39485524, ; 3: System.Net.WebSockets.dll => 0x25a8054 => 103
	i32 42639949, ; 4: System.Threading.Thread => 0x28aa24d => 117
	i32 67008169, ; 5: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 33
	i32 72070932, ; 6: Microsoft.Maui.Graphics.dll => 0x44bb714 => 47
	i32 117431740, ; 7: System.Runtime.InteropServices => 0x6ffddbc => 108
	i32 122350210, ; 8: System.Threading.Channels.dll => 0x74aea82 => 116
	i32 142721839, ; 9: System.Net.WebHeaderCollection => 0x881c32f => 101
	i32 182336117, ; 10: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 71
	i32 195452805, ; 11: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 30
	i32 199333315, ; 12: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 31
	i32 205061960, ; 13: System.ComponentModel => 0xc38ff48 => 84
	i32 280992041, ; 14: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 2
	i32 317674968, ; 15: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 30
	i32 318968648, ; 16: Xamarin.AndroidX.Activity.dll => 0x13031348 => 49
	i32 336156722, ; 17: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 15
	i32 342366114, ; 18: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 60
	i32 356389973, ; 19: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 14
	i32 367216257, ; 20: Plugin.AudioRecorder => 0x15e34681 => 48
	i32 379916513, ; 21: System.Threading.Thread.dll => 0x16a510e1 => 117
	i32 385762202, ; 22: System.Memory.dll => 0x16fe439a => 92
	i32 395744057, ; 23: _Microsoft.Android.Resource.Designer => 0x17969339 => 34
	i32 435591531, ; 24: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 26
	i32 442565967, ; 25: System.Collections => 0x1a61054f => 81
	i32 450948140, ; 26: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 59
	i32 465846621, ; 27: mscorlib => 0x1bc4415d => 121
	i32 469710990, ; 28: System.dll => 0x1bff388e => 120
	i32 498788369, ; 29: System.ObjectModel => 0x1dbae811 => 105
	i32 500358224, ; 30: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 13
	i32 503918385, ; 31: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 7
	i32 513247710, ; 32: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 42
	i32 539058512, ; 33: Microsoft.Extensions.Logging => 0x20216150 => 39
	i32 592146354, ; 34: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 21
	i32 627609679, ; 35: Xamarin.AndroidX.CustomView => 0x2568904f => 57
	i32 627931235, ; 36: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 19
	i32 662205335, ; 37: System.Text.Encodings.Web.dll => 0x27787397 => 113
	i32 672442732, ; 38: System.Collections.Concurrent => 0x2814a96c => 78
	i32 683518922, ; 39: System.Net.Security => 0x28bdabca => 99
	i32 688181140, ; 40: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 1
	i32 706645707, ; 41: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 16
	i32 709557578, ; 42: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 4
	i32 722857257, ; 43: System.Runtime.Loader.dll => 0x2b15ed29 => 109
	i32 759454413, ; 44: System.Net.Requests => 0x2d445acd => 98
	i32 775507847, ; 45: System.IO.Compression => 0x2e394f87 => 89
	i32 777317022, ; 46: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 25
	i32 789151979, ; 47: Microsoft.Extensions.Options => 0x2f0980eb => 41
	i32 823281589, ; 48: System.Private.Uri.dll => 0x311247b5 => 106
	i32 830298997, ; 49: System.IO.Compression.Brotli => 0x317d5b75 => 88
	i32 878954865, ; 50: System.Net.Http.Json => 0x3463c971 => 93
	i32 904024072, ; 51: System.ComponentModel.Primitives.dll => 0x35e25008 => 82
	i32 925467409, ; 52: FraudGuardAI.dll => 0x37298311 => 77
	i32 926902833, ; 53: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 28
	i32 967690846, ; 54: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 60
	i32 992768348, ; 55: System.Collections.dll => 0x3b2c715c => 81
	i32 1012816738, ; 56: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 70
	i32 1028951442, ; 57: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 38
	i32 1029334545, ; 58: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 3
	i32 1035644815, ; 59: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 50
	i32 1044663988, ; 60: System.Linq.Expressions.dll => 0x3e444eb4 => 90
	i32 1052210849, ; 61: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 62
	i32 1082857460, ; 62: System.ComponentModel.TypeConverter => 0x408b17f4 => 83
	i32 1084122840, ; 63: Xamarin.Kotlin.StdLib => 0x409e66d8 => 75
	i32 1098259244, ; 64: System => 0x41761b2c => 120
	i32 1118262833, ; 65: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 16
	i32 1168523401, ; 66: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 22
	i32 1178241025, ; 67: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 67
	i32 1203215381, ; 68: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 20
	i32 1234928153, ; 69: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 18
	i32 1246548578, ; 70: Xamarin.AndroidX.Collection.Jvm.dll => 0x4a4cd262 => 53
	i32 1260983243, ; 71: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 2
	i32 1293217323, ; 72: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 58
	i32 1324164729, ; 73: System.Linq => 0x4eed2679 => 91
	i32 1373134921, ; 74: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 32
	i32 1376866003, ; 75: Xamarin.AndroidX.SavedState => 0x52114ed3 => 70
	i32 1406073936, ; 76: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 54
	i32 1430672901, ; 77: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 0
	i32 1452070440, ; 78: System.Formats.Asn1.dll => 0x568cd628 => 87
	i32 1458022317, ; 79: System.Net.Security.dll => 0x56e7a7ad => 99
	i32 1461004990, ; 80: es\Microsoft.Maui.Controls.resources => 0x57152abe => 6
	i32 1462112819, ; 81: System.IO.Compression.dll => 0x57261233 => 89
	i32 1469204771, ; 82: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 51
	i32 1470490898, ; 83: Microsoft.Extensions.Primitives => 0x57a5e912 => 42
	i32 1480492111, ; 84: System.IO.Compression.Brotli.dll => 0x583e844f => 88
	i32 1493001747, ; 85: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 10
	i32 1514721132, ; 86: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 5
	i32 1543031311, ; 87: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 115
	i32 1551623176, ; 88: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 25
	i32 1593325549, ; 89: FraudGuardAI => 0x5ef837ed => 77
	i32 1618516317, ; 90: System.Net.WebSockets.Client.dll => 0x6078995d => 102
	i32 1622152042, ; 91: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 64
	i32 1624863272, ; 92: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 73
	i32 1636350590, ; 93: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 56
	i32 1639515021, ; 94: System.Net.Http.dll => 0x61b9038d => 94
	i32 1639986890, ; 95: System.Text.RegularExpressions => 0x61c036ca => 115
	i32 1657153582, ; 96: System.Runtime => 0x62c6282e => 111
	i32 1658251792, ; 97: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 74
	i32 1677501392, ; 98: System.Net.Primitives.dll => 0x63fca3d0 => 97
	i32 1678508291, ; 99: System.Net.WebSockets => 0x640c0103 => 103
	i32 1679769178, ; 100: System.Security.Cryptography => 0x641f3e5a => 112
	i32 1729485958, ; 101: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 52
	i32 1736233607, ; 102: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 23
	i32 1743415430, ; 103: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 1
	i32 1766324549, ; 104: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 71
	i32 1770582343, ; 105: Microsoft.Extensions.Logging.dll => 0x6988f147 => 39
	i32 1780572499, ; 106: Mono.Android.Runtime.dll => 0x6a216153 => 124
	i32 1782862114, ; 107: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 17
	i32 1788241197, ; 108: Xamarin.AndroidX.Fragment => 0x6a96652d => 59
	i32 1793755602, ; 109: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 9
	i32 1808609942, ; 110: Xamarin.AndroidX.Loader => 0x6bcd3296 => 64
	i32 1813058853, ; 111: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 75
	i32 1813201214, ; 112: Xamarin.Google.Android.Material => 0x6c13413e => 74
	i32 1818569960, ; 113: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 68
	i32 1828688058, ; 114: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 40
	i32 1842015223, ; 115: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 29
	i32 1853025655, ; 116: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 26
	i32 1858542181, ; 117: System.Linq.Expressions => 0x6ec71a65 => 90
	i32 1875935024, ; 118: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 8
	i32 1910275211, ; 119: System.Collections.NonGeneric.dll => 0x71dc7c8b => 79
	i32 1968388702, ; 120: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 35
	i32 2003115576, ; 121: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 5
	i32 2019465201, ; 122: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 62
	i32 2025202353, ; 123: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 0
	i32 2045470958, ; 124: System.Private.Xml => 0x79eb68ee => 107
	i32 2055257422, ; 125: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 61
	i32 2066184531, ; 126: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 4
	i32 2079903147, ; 127: System.Runtime.dll => 0x7bf8cdab => 111
	i32 2090596640, ; 128: System.Numerics.Vectors => 0x7c9bf920 => 104
	i32 2127167465, ; 129: System.Console => 0x7ec9ffe9 => 85
	i32 2142473426, ; 130: System.Collections.Specialized => 0x7fb38cd2 => 80
	i32 2159891885, ; 131: Microsoft.Maui => 0x80bd55ad => 45
	i32 2169148018, ; 132: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 12
	i32 2181898931, ; 133: Microsoft.Extensions.Options.dll => 0x820d22b3 => 41
	i32 2192057212, ; 134: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 40
	i32 2193016926, ; 135: System.ObjectModel.dll => 0x82b6c85e => 105
	i32 2201107256, ; 136: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 76
	i32 2201231467, ; 137: System.Net.Http => 0x8334206b => 94
	i32 2207618523, ; 138: it\Microsoft.Maui.Controls.resources => 0x839595db => 14
	i32 2266799131, ; 139: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 36
	i32 2270573516, ; 140: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 8
	i32 2279755925, ; 141: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 69
	i32 2295906218, ; 142: System.Net.Sockets => 0x88d8bfaa => 100
	i32 2303942373, ; 143: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 18
	i32 2305521784, ; 144: System.Private.CoreLib.dll => 0x896b7878 => 122
	i32 2353062107, ; 145: System.Net.Primitives => 0x8c40e0db => 97
	i32 2368005991, ; 146: System.Xml.ReaderWriter.dll => 0x8d24e767 => 119
	i32 2371007202, ; 147: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 35
	i32 2395872292, ; 148: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 13
	i32 2427813419, ; 149: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 10
	i32 2435356389, ; 150: System.Console.dll => 0x912896e5 => 85
	i32 2458678730, ; 151: System.Net.Sockets.dll => 0x928c75ca => 100
	i32 2475788418, ; 152: Java.Interop.dll => 0x93918882 => 123
	i32 2480646305, ; 153: Microsoft.Maui.Controls => 0x93dba8a1 => 43
	i32 2550873716, ; 154: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 11
	i32 2570120770, ; 155: System.Text.Encodings.Web => 0x9930ee42 => 113
	i32 2593496499, ; 156: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 20
	i32 2605712449, ; 157: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 76
	i32 2617129537, ; 158: System.Private.Xml.dll => 0x9bfe3a41 => 107
	i32 2620871830, ; 159: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 56
	i32 2626831493, ; 160: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 15
	i32 2663698177, ; 161: System.Runtime.Loader => 0x9ec4cf01 => 109
	i32 2724373263, ; 162: System.Runtime.Numerics.dll => 0xa262a30f => 110
	i32 2732626843, ; 163: Xamarin.AndroidX.Activity => 0xa2e0939b => 49
	i32 2735172069, ; 164: System.Threading.Channels => 0xa30769e5 => 116
	i32 2737747696, ; 165: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 51
	i32 2752995522, ; 166: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 21
	i32 2758225723, ; 167: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 44
	i32 2764765095, ; 168: Microsoft.Maui.dll => 0xa4caf7a7 => 45
	i32 2778768386, ; 169: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 72
	i32 2785988530, ; 170: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 27
	i32 2801831435, ; 171: Microsoft.Maui.Graphics => 0xa7008e0b => 47
	i32 2806116107, ; 172: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 6
	i32 2810250172, ; 173: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 54
	i32 2831556043, ; 174: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 19
	i32 2853208004, ; 175: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 72
	i32 2861189240, ; 176: Microsoft.Maui.Essentials => 0xaa8a4878 => 46
	i32 2905242038, ; 177: mscorlib.dll => 0xad2a79b6 => 121
	i32 2909740682, ; 178: System.Private.CoreLib => 0xad6f1e8a => 122
	i32 2916838712, ; 179: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 73
	i32 2919462931, ; 180: System.Numerics.Vectors.dll => 0xae037813 => 104
	i32 2959614098, ; 181: System.ComponentModel.dll => 0xb0682092 => 84
	i32 2978675010, ; 182: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 58
	i32 3038032645, ; 183: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 34
	i32 3057625584, ; 184: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 65
	i32 3059408633, ; 185: Mono.Android.Runtime => 0xb65adef9 => 124
	i32 3059793426, ; 186: System.ComponentModel.Primitives => 0xb660be12 => 82
	i32 3077302341, ; 187: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 12
	i32 3103600923, ; 188: System.Formats.Asn1 => 0xb8fd311b => 87
	i32 3178803400, ; 189: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 66
	i32 3220365878, ; 190: System.Threading => 0xbff2e236 => 118
	i32 3258312781, ; 191: Xamarin.AndroidX.CardView => 0xc235e84d => 52
	i32 3305363605, ; 192: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 7
	i32 3316684772, ; 193: System.Net.Requests.dll => 0xc5b097e4 => 98
	i32 3317135071, ; 194: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 57
	i32 3346324047, ; 195: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 67
	i32 3357674450, ; 196: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 24
	i32 3358260929, ; 197: System.Text.Json => 0xc82afec1 => 114
	i32 3362522851, ; 198: Xamarin.AndroidX.Core => 0xc86c06e3 => 55
	i32 3366347497, ; 199: Java.Interop => 0xc8a662e9 => 123
	i32 3374999561, ; 200: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 69
	i32 3381016424, ; 201: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 3
	i32 3428513518, ; 202: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 37
	i32 3463511458, ; 203: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 11
	i32 3471940407, ; 204: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 83
	i32 3476120550, ; 205: Mono.Android => 0xcf3163e6 => 125
	i32 3479583265, ; 206: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 24
	i32 3484440000, ; 207: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 23
	i32 3485117614, ; 208: System.Text.Json.dll => 0xcfbaacae => 114
	i32 3580758918, ; 209: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 31
	i32 3598340787, ; 210: System.Net.WebSockets.Client => 0xd67a52b3 => 102
	i32 3608519521, ; 211: System.Linq.dll => 0xd715a361 => 91
	i32 3641597786, ; 212: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 61
	i32 3643446276, ; 213: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 28
	i32 3643854240, ; 214: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 66
	i32 3657292374, ; 215: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 36
	i32 3660523487, ; 216: System.Net.NetworkInformation => 0xda2f27df => 96
	i32 3672681054, ; 217: Mono.Android.dll => 0xdae8aa5e => 125
	i32 3697841164, ; 218: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 33
	i32 3724971120, ; 219: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 65
	i32 3732100267, ; 220: System.Net.NameResolution => 0xde7354ab => 95
	i32 3737834244, ; 221: System.Net.Http.Json.dll => 0xdecad304 => 93
	i32 3748608112, ; 222: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 86
	i32 3792276235, ; 223: System.Collections.NonGeneric => 0xe2098b0b => 79
	i32 3802395368, ; 224: System.Collections.Specialized.dll => 0xe2a3f2e8 => 80
	i32 3823082795, ; 225: System.Security.Cryptography.dll => 0xe3df9d2b => 112
	i32 3841636137, ; 226: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 38
	i32 3849253459, ; 227: System.Runtime.InteropServices.dll => 0xe56ef253 => 108
	i32 3885497537, ; 228: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 101
	i32 3889960447, ; 229: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 32
	i32 3896106733, ; 230: System.Collections.Concurrent.dll => 0xe839deed => 78
	i32 3896760992, ; 231: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 55
	i32 3910130544, ; 232: Xamarin.AndroidX.Collection.Jvm => 0xe90fdb70 => 53
	i32 3928044579, ; 233: System.Xml.ReaderWriter => 0xea213423 => 119
	i32 3931092270, ; 234: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 68
	i32 3955647286, ; 235: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 50
	i32 3980434154, ; 236: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 27
	i32 3987592930, ; 237: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 9
	i32 4025784931, ; 238: System.Memory => 0xeff49a63 => 92
	i32 4046471985, ; 239: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 44
	i32 4073602200, ; 240: System.Threading.dll => 0xf2ce3c98 => 118
	i32 4094352644, ; 241: Microsoft.Maui.Essentials.dll => 0xf40add04 => 46
	i32 4100113165, ; 242: System.Private.Uri => 0xf462c30d => 106
	i32 4102112229, ; 243: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 22
	i32 4125707920, ; 244: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 17
	i32 4126470640, ; 245: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 37
	i32 4150914736, ; 246: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 29
	i32 4182413190, ; 247: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 63
	i32 4213026141, ; 248: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 86
	i32 4271975918, ; 249: Microsoft.Maui.Controls.dll => 0xfea12dee => 43
	i32 4274976490, ; 250: System.Runtime.Numerics => 0xfecef6ea => 110
	i32 4292120959 ; 251: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 63
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [252 x i32] [
	i32 96, ; 0
	i32 95, ; 1
	i32 48, ; 2
	i32 103, ; 3
	i32 117, ; 4
	i32 33, ; 5
	i32 47, ; 6
	i32 108, ; 7
	i32 116, ; 8
	i32 101, ; 9
	i32 71, ; 10
	i32 30, ; 11
	i32 31, ; 12
	i32 84, ; 13
	i32 2, ; 14
	i32 30, ; 15
	i32 49, ; 16
	i32 15, ; 17
	i32 60, ; 18
	i32 14, ; 19
	i32 48, ; 20
	i32 117, ; 21
	i32 92, ; 22
	i32 34, ; 23
	i32 26, ; 24
	i32 81, ; 25
	i32 59, ; 26
	i32 121, ; 27
	i32 120, ; 28
	i32 105, ; 29
	i32 13, ; 30
	i32 7, ; 31
	i32 42, ; 32
	i32 39, ; 33
	i32 21, ; 34
	i32 57, ; 35
	i32 19, ; 36
	i32 113, ; 37
	i32 78, ; 38
	i32 99, ; 39
	i32 1, ; 40
	i32 16, ; 41
	i32 4, ; 42
	i32 109, ; 43
	i32 98, ; 44
	i32 89, ; 45
	i32 25, ; 46
	i32 41, ; 47
	i32 106, ; 48
	i32 88, ; 49
	i32 93, ; 50
	i32 82, ; 51
	i32 77, ; 52
	i32 28, ; 53
	i32 60, ; 54
	i32 81, ; 55
	i32 70, ; 56
	i32 38, ; 57
	i32 3, ; 58
	i32 50, ; 59
	i32 90, ; 60
	i32 62, ; 61
	i32 83, ; 62
	i32 75, ; 63
	i32 120, ; 64
	i32 16, ; 65
	i32 22, ; 66
	i32 67, ; 67
	i32 20, ; 68
	i32 18, ; 69
	i32 53, ; 70
	i32 2, ; 71
	i32 58, ; 72
	i32 91, ; 73
	i32 32, ; 74
	i32 70, ; 75
	i32 54, ; 76
	i32 0, ; 77
	i32 87, ; 78
	i32 99, ; 79
	i32 6, ; 80
	i32 89, ; 81
	i32 51, ; 82
	i32 42, ; 83
	i32 88, ; 84
	i32 10, ; 85
	i32 5, ; 86
	i32 115, ; 87
	i32 25, ; 88
	i32 77, ; 89
	i32 102, ; 90
	i32 64, ; 91
	i32 73, ; 92
	i32 56, ; 93
	i32 94, ; 94
	i32 115, ; 95
	i32 111, ; 96
	i32 74, ; 97
	i32 97, ; 98
	i32 103, ; 99
	i32 112, ; 100
	i32 52, ; 101
	i32 23, ; 102
	i32 1, ; 103
	i32 71, ; 104
	i32 39, ; 105
	i32 124, ; 106
	i32 17, ; 107
	i32 59, ; 108
	i32 9, ; 109
	i32 64, ; 110
	i32 75, ; 111
	i32 74, ; 112
	i32 68, ; 113
	i32 40, ; 114
	i32 29, ; 115
	i32 26, ; 116
	i32 90, ; 117
	i32 8, ; 118
	i32 79, ; 119
	i32 35, ; 120
	i32 5, ; 121
	i32 62, ; 122
	i32 0, ; 123
	i32 107, ; 124
	i32 61, ; 125
	i32 4, ; 126
	i32 111, ; 127
	i32 104, ; 128
	i32 85, ; 129
	i32 80, ; 130
	i32 45, ; 131
	i32 12, ; 132
	i32 41, ; 133
	i32 40, ; 134
	i32 105, ; 135
	i32 76, ; 136
	i32 94, ; 137
	i32 14, ; 138
	i32 36, ; 139
	i32 8, ; 140
	i32 69, ; 141
	i32 100, ; 142
	i32 18, ; 143
	i32 122, ; 144
	i32 97, ; 145
	i32 119, ; 146
	i32 35, ; 147
	i32 13, ; 148
	i32 10, ; 149
	i32 85, ; 150
	i32 100, ; 151
	i32 123, ; 152
	i32 43, ; 153
	i32 11, ; 154
	i32 113, ; 155
	i32 20, ; 156
	i32 76, ; 157
	i32 107, ; 158
	i32 56, ; 159
	i32 15, ; 160
	i32 109, ; 161
	i32 110, ; 162
	i32 49, ; 163
	i32 116, ; 164
	i32 51, ; 165
	i32 21, ; 166
	i32 44, ; 167
	i32 45, ; 168
	i32 72, ; 169
	i32 27, ; 170
	i32 47, ; 171
	i32 6, ; 172
	i32 54, ; 173
	i32 19, ; 174
	i32 72, ; 175
	i32 46, ; 176
	i32 121, ; 177
	i32 122, ; 178
	i32 73, ; 179
	i32 104, ; 180
	i32 84, ; 181
	i32 58, ; 182
	i32 34, ; 183
	i32 65, ; 184
	i32 124, ; 185
	i32 82, ; 186
	i32 12, ; 187
	i32 87, ; 188
	i32 66, ; 189
	i32 118, ; 190
	i32 52, ; 191
	i32 7, ; 192
	i32 98, ; 193
	i32 57, ; 194
	i32 67, ; 195
	i32 24, ; 196
	i32 114, ; 197
	i32 55, ; 198
	i32 123, ; 199
	i32 69, ; 200
	i32 3, ; 201
	i32 37, ; 202
	i32 11, ; 203
	i32 83, ; 204
	i32 125, ; 205
	i32 24, ; 206
	i32 23, ; 207
	i32 114, ; 208
	i32 31, ; 209
	i32 102, ; 210
	i32 91, ; 211
	i32 61, ; 212
	i32 28, ; 213
	i32 66, ; 214
	i32 36, ; 215
	i32 96, ; 216
	i32 125, ; 217
	i32 33, ; 218
	i32 65, ; 219
	i32 95, ; 220
	i32 93, ; 221
	i32 86, ; 222
	i32 79, ; 223
	i32 80, ; 224
	i32 112, ; 225
	i32 38, ; 226
	i32 108, ; 227
	i32 101, ; 228
	i32 32, ; 229
	i32 78, ; 230
	i32 55, ; 231
	i32 53, ; 232
	i32 119, ; 233
	i32 68, ; 234
	i32 50, ; 235
	i32 27, ; 236
	i32 9, ; 237
	i32 92, ; 238
	i32 44, ; 239
	i32 118, ; 240
	i32 46, ; 241
	i32 106, ; 242
	i32 22, ; 243
	i32 17, ; 244
	i32 37, ; 245
	i32 29, ; 246
	i32 63, ; 247
	i32 86, ; 248
	i32 43, ; 249
	i32 110, ; 250
	i32 63 ; 251
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
