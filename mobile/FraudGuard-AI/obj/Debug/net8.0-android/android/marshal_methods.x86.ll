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

@assembly_image_cache = dso_local local_unnamed_addr global [329 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [652 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 68
	i32 10166715, ; 1: System.Net.NameResolution.dll => 0x9b21bb => 67
	i32 14013194, ; 2: Plugin.AudioRecorder.dll => 0xd5d30a => 189
	i32 15721112, ; 3: System.Runtime.Intrinsics.dll => 0xefe298 => 108
	i32 32687329, ; 4: Xamarin.AndroidX.Lifecycle.Runtime => 0x1f2c4e1 => 234
	i32 34715100, ; 5: Xamarin.Google.Guava.ListenableFuture.dll => 0x211b5dc => 277
	i32 34839235, ; 6: System.IO.FileSystem.DriveInfo => 0x2139ac3 => 48
	i32 39485524, ; 7: System.Net.WebSockets.dll => 0x25a8054 => 80
	i32 42639949, ; 8: System.Threading.Thread => 0x28aa24d => 145
	i32 57305218, ; 9: Xamarin.KotlinX.Coroutines.Play.Services => 0x36a6882 => 290
	i32 66541672, ; 10: System.Diagnostics.StackTrace => 0x3f75868 => 30
	i32 67008169, ; 11: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 324
	i32 68219467, ; 12: System.Security.Cryptography.Primitives => 0x410f24b => 124
	i32 72070932, ; 13: Microsoft.Maui.Graphics.dll => 0x44bb714 => 188
	i32 82292897, ; 14: System.Runtime.CompilerServices.VisualC.dll => 0x4e7b0a1 => 102
	i32 101534019, ; 15: Xamarin.AndroidX.SlidingPaneLayout => 0x60d4943 => 252
	i32 103834273, ; 16: Xamarin.Firebase.Annotations.dll => 0x63062a1 => 264
	i32 117431740, ; 17: System.Runtime.InteropServices => 0x6ffddbc => 107
	i32 120558881, ; 18: Xamarin.AndroidX.SlidingPaneLayout.dll => 0x72f9521 => 252
	i32 122350210, ; 19: System.Threading.Channels.dll => 0x74aea82 => 139
	i32 134690465, ; 20: Xamarin.Kotlin.StdLib.Jdk7.dll => 0x80736a1 => 286
	i32 142721839, ; 21: System.Net.WebHeaderCollection => 0x881c32f => 77
	i32 147669188, ; 22: Plugin.Firebase.Core.dll => 0x8cd40c4 => 191
	i32 149972175, ; 23: System.Security.Cryptography.Primitives.dll => 0x8f064cf => 124
	i32 159306688, ; 24: System.ComponentModel.Annotations => 0x97ed3c0 => 13
	i32 165246403, ; 25: Xamarin.AndroidX.Collection.dll => 0x9d975c3 => 207
	i32 176265551, ; 26: System.ServiceProcess => 0xa81994f => 132
	i32 182336117, ; 27: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 254
	i32 184328833, ; 28: System.ValueTuple.dll => 0xafca281 => 151
	i32 195452805, ; 29: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 321
	i32 199333315, ; 30: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 322
	i32 205061960, ; 31: System.ComponentModel => 0xc38ff48 => 18
	i32 209399409, ; 32: Xamarin.AndroidX.Browser.dll => 0xc7b2e71 => 205
	i32 220171995, ; 33: System.Diagnostics.Debug => 0xd1f8edb => 26
	i32 230216969, ; 34: Xamarin.AndroidX.Legacy.Support.Core.Utils.dll => 0xdb8d509 => 228
	i32 230752869, ; 35: Microsoft.CSharp.dll => 0xdc10265 => 1
	i32 231409092, ; 36: System.Linq.Parallel => 0xdcb05c4 => 59
	i32 231814094, ; 37: System.Globalization => 0xdd133ce => 42
	i32 246610117, ; 38: System.Reflection.Emit.Lightweight => 0xeb2f8c5 => 91
	i32 261689757, ; 39: Xamarin.AndroidX.ConstraintLayout.dll => 0xf99119d => 211
	i32 276479776, ; 40: System.Threading.Timer.dll => 0x107abf20 => 147
	i32 278686392, ; 41: Xamarin.AndroidX.Lifecycle.LiveData.dll => 0x109c6ab8 => 230
	i32 280482487, ; 42: Xamarin.AndroidX.Interpolator => 0x10b7d2b7 => 227
	i32 280992041, ; 43: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 293
	i32 291076382, ; 44: System.IO.Pipes.AccessControl.dll => 0x1159791e => 54
	i32 293936332, ; 45: Xamarin.GooglePlayServices.Auth.Api.Phone.dll => 0x11851ccc => 278
	i32 298918909, ; 46: System.Net.Ping.dll => 0x11d123fd => 69
	i32 317674968, ; 47: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 321
	i32 318968648, ; 48: Xamarin.AndroidX.Activity.dll => 0x13031348 => 196
	i32 321597661, ; 49: System.Numerics => 0x132b30dd => 83
	i32 336156722, ; 50: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 306
	i32 342366114, ; 51: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 229
	i32 356389973, ; 52: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 305
	i32 360082299, ; 53: System.ServiceModel.Web => 0x15766b7b => 131
	i32 367216257, ; 54: Plugin.AudioRecorder => 0x15e34681 => 189
	i32 367780167, ; 55: System.IO.Pipes => 0x15ebe147 => 55
	i32 374914964, ; 56: System.Transactions.Local => 0x1658bf94 => 149
	i32 375677976, ; 57: System.Net.ServicePoint.dll => 0x16646418 => 74
	i32 379916513, ; 58: System.Threading.Thread.dll => 0x16a510e1 => 145
	i32 385762202, ; 59: System.Memory.dll => 0x16fe439a => 62
	i32 392610295, ; 60: System.Threading.ThreadPool.dll => 0x1766c1f7 => 146
	i32 395744057, ; 61: _Microsoft.Android.Resource.Designer => 0x17969339 => 325
	i32 403441872, ; 62: WindowsBase => 0x180c08d0 => 165
	i32 435591531, ; 63: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 317
	i32 441335492, ; 64: Xamarin.AndroidX.ConstraintLayout.Core => 0x1a4e3ec4 => 212
	i32 442565967, ; 65: System.Collections => 0x1a61054f => 12
	i32 443493152, ; 66: Xamarin.Google.Android.Recaptcha => 0x1a6f2b20 => 273
	i32 450948140, ; 67: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 225
	i32 451504562, ; 68: System.Security.Cryptography.X509Certificates => 0x1ae969b2 => 125
	i32 456227837, ; 69: System.Web.HttpUtility.dll => 0x1b317bfd => 152
	i32 459347974, ; 70: System.Runtime.Serialization.Primitives.dll => 0x1b611806 => 113
	i32 465846621, ; 71: mscorlib => 0x1bc4415d => 166
	i32 469710990, ; 72: System.dll => 0x1bff388e => 164
	i32 476646585, ; 73: Xamarin.AndroidX.Interpolator.dll => 0x1c690cb9 => 227
	i32 486930444, ; 74: Xamarin.AndroidX.LocalBroadcastManager.dll => 0x1d05f80c => 240
	i32 493301629, ; 75: Xamarin.Firebase.AppCheck.Interop.dll => 0x1d672f7d => 265
	i32 498788369, ; 76: System.ObjectModel => 0x1dbae811 => 84
	i32 500358224, ; 77: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 304
	i32 503918385, ; 78: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 298
	i32 513247710, ; 79: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 182
	i32 526420162, ; 80: System.Transactions.dll => 0x1f6088c2 => 150
	i32 527452488, ; 81: Xamarin.Kotlin.StdLib.Jdk7 => 0x1f704948 => 286
	i32 530272170, ; 82: System.Linq.Queryable => 0x1f9b4faa => 60
	i32 539058512, ; 83: Microsoft.Extensions.Logging => 0x20216150 => 178
	i32 540030774, ; 84: System.IO.FileSystem.dll => 0x20303736 => 51
	i32 545304856, ; 85: System.Runtime.Extensions => 0x2080b118 => 103
	i32 546455878, ; 86: System.Runtime.Serialization.Xml => 0x20924146 => 114
	i32 549171840, ; 87: System.Globalization.Calendars => 0x20bbb280 => 40
	i32 557405415, ; 88: Jsr305Binding => 0x213954e7 => 274
	i32 569601784, ; 89: Xamarin.AndroidX.Window.Extensions.Core.Core => 0x21f36ef8 => 263
	i32 577335427, ; 90: System.Security.Cryptography.Cng => 0x22697083 => 120
	i32 589597883, ; 91: Xamarin.GooglePlayServices.Auth.Api.Phone => 0x23248cbb => 278
	i32 592146354, ; 92: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 312
	i32 601371474, ; 93: System.IO.IsolatedStorage.dll => 0x23d83352 => 52
	i32 605376203, ; 94: System.IO.Compression.FileSystem => 0x24154ecb => 44
	i32 613668793, ; 95: System.Security.Cryptography.Algorithms => 0x2493d7b9 => 119
	i32 627609679, ; 96: Xamarin.AndroidX.CustomView => 0x2568904f => 217
	i32 627931235, ; 97: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 310
	i32 639843206, ; 98: Xamarin.AndroidX.Emoji2.ViewsHelper.dll => 0x26233b86 => 223
	i32 643868501, ; 99: System.Net => 0x2660a755 => 81
	i32 662205335, ; 100: System.Text.Encodings.Web.dll => 0x27787397 => 136
	i32 663517072, ; 101: Xamarin.AndroidX.VersionedParcelable => 0x278c7790 => 259
	i32 666292255, ; 102: Xamarin.AndroidX.Arch.Core.Common.dll => 0x27b6d01f => 203
	i32 672442732, ; 103: System.Collections.Concurrent => 0x2814a96c => 8
	i32 683518922, ; 104: System.Net.Security => 0x28bdabca => 73
	i32 688181140, ; 105: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 292
	i32 690569205, ; 106: System.Xml.Linq.dll => 0x29293ff5 => 155
	i32 691348768, ; 107: Xamarin.KotlinX.Coroutines.Android.dll => 0x29352520 => 288
	i32 693804605, ; 108: System.Windows => 0x295a9e3d => 154
	i32 699345723, ; 109: System.Reflection.Emit => 0x29af2b3b => 92
	i32 700284507, ; 110: Xamarin.Jetbrains.Annotations => 0x29bd7e5b => 283
	i32 700358131, ; 111: System.IO.Compression.ZipFile => 0x29be9df3 => 45
	i32 706645707, ; 112: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 307
	i32 709557578, ; 113: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 295
	i32 720511267, ; 114: Xamarin.Kotlin.StdLib.Jdk8 => 0x2af22123 => 287
	i32 722857257, ; 115: System.Runtime.Loader.dll => 0x2b15ed29 => 109
	i32 735137430, ; 116: System.Security.SecureString.dll => 0x2bd14e96 => 129
	i32 752232764, ; 117: System.Diagnostics.Contracts.dll => 0x2cd6293c => 25
	i32 755313932, ; 118: Xamarin.Android.Glide.Annotations.dll => 0x2d052d0c => 193
	i32 759454413, ; 119: System.Net.Requests => 0x2d445acd => 72
	i32 762598435, ; 120: System.IO.Pipes.dll => 0x2d745423 => 55
	i32 763781610, ; 121: Xamarin.Google.Android.Play.Integrity => 0x2d8661ea => 272
	i32 775507847, ; 122: System.IO.Compression => 0x2e394f87 => 46
	i32 777317022, ; 123: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 316
	i32 789151979, ; 124: Microsoft.Extensions.Options => 0x2f0980eb => 181
	i32 790371945, ; 125: Xamarin.AndroidX.CustomView.PoolingContainer.dll => 0x2f1c1e69 => 218
	i32 804715423, ; 126: System.Data.Common => 0x2ff6fb9f => 22
	i32 807930345, ; 127: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll => 0x302809e9 => 232
	i32 823281589, ; 128: System.Private.Uri.dll => 0x311247b5 => 86
	i32 830298997, ; 129: System.IO.Compression.Brotli => 0x317d5b75 => 43
	i32 832635846, ; 130: System.Xml.XPath.dll => 0x31a103c6 => 160
	i32 834051424, ; 131: System.Net.Quic => 0x31b69d60 => 71
	i32 843511501, ; 132: Xamarin.AndroidX.Print => 0x3246f6cd => 245
	i32 873119928, ; 133: Microsoft.VisualBasic => 0x340ac0b8 => 3
	i32 877678880, ; 134: System.Globalization.dll => 0x34505120 => 42
	i32 878954865, ; 135: System.Net.Http.Json => 0x3463c971 => 63
	i32 904024072, ; 136: System.ComponentModel.Primitives.dll => 0x35e25008 => 16
	i32 911108515, ; 137: System.IO.MemoryMappedFiles.dll => 0x364e69a3 => 53
	i32 925467409, ; 138: FraudGuardAI.dll => 0x37298311 => 0
	i32 926902833, ; 139: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 319
	i32 928116545, ; 140: Xamarin.Google.Guava.ListenableFuture => 0x3751ef41 => 277
	i32 952186615, ; 141: System.Runtime.InteropServices.JavaScript.dll => 0x38c136f7 => 105
	i32 956575887, ; 142: Xamarin.Kotlin.StdLib.Jdk8.dll => 0x3904308f => 287
	i32 965247473, ; 143: Plugin.Firebase.Core => 0x398881f1 => 191
	i32 966729478, ; 144: Xamarin.Google.Crypto.Tink.Android => 0x399f1f06 => 275
	i32 967690846, ; 145: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 229
	i32 975236339, ; 146: System.Diagnostics.Tracing => 0x3a20ecf3 => 34
	i32 975874589, ; 147: System.Xml.XDocument => 0x3a2aaa1d => 158
	i32 986514023, ; 148: System.Private.DataContractSerialization.dll => 0x3acd0267 => 85
	i32 987214855, ; 149: System.Diagnostics.Tools => 0x3ad7b407 => 32
	i32 992768348, ; 150: System.Collections.dll => 0x3b2c715c => 12
	i32 994442037, ; 151: System.IO.FileSystem => 0x3b45fb35 => 51
	i32 1001831731, ; 152: System.IO.UnmanagedMemoryStream.dll => 0x3bb6bd33 => 56
	i32 1012816738, ; 153: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 249
	i32 1019214401, ; 154: System.Drawing => 0x3cbffa41 => 36
	i32 1028951442, ; 155: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 177
	i32 1029334545, ; 156: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 294
	i32 1031528504, ; 157: Xamarin.Google.ErrorProne.Annotations.dll => 0x3d7be038 => 276
	i32 1035644815, ; 158: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 201
	i32 1036536393, ; 159: System.Drawing.Primitives.dll => 0x3dc84a49 => 35
	i32 1044663988, ; 160: System.Linq.Expressions.dll => 0x3e444eb4 => 58
	i32 1052210849, ; 161: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 236
	i32 1067306892, ; 162: GoogleGson => 0x3f9dcf8c => 173
	i32 1082857460, ; 163: System.ComponentModel.TypeConverter => 0x408b17f4 => 17
	i32 1084122840, ; 164: Xamarin.Kotlin.StdLib => 0x409e66d8 => 284
	i32 1098259244, ; 165: System => 0x41761b2c => 164
	i32 1110581358, ; 166: Xamarin.Firebase.Auth => 0x4232206e => 266
	i32 1118262833, ; 167: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 307
	i32 1121599056, ; 168: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll => 0x42da3e50 => 235
	i32 1127624469, ; 169: Microsoft.Extensions.Logging.Debug => 0x43362f15 => 180
	i32 1149092582, ; 170: Xamarin.AndroidX.Window => 0x447dc2e6 => 262
	i32 1168523401, ; 171: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 313
	i32 1170634674, ; 172: System.Web.dll => 0x45c677b2 => 153
	i32 1175144683, ; 173: Xamarin.AndroidX.VectorDrawable.Animated => 0x460b48eb => 258
	i32 1178241025, ; 174: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 243
	i32 1203215381, ; 175: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 311
	i32 1204270330, ; 176: Xamarin.AndroidX.Arch.Core.Common => 0x47c7b4fa => 203
	i32 1208641965, ; 177: System.Diagnostics.Process => 0x480a69ad => 29
	i32 1219128291, ; 178: System.IO.IsolatedStorage => 0x48aa6be3 => 52
	i32 1234928153, ; 179: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 309
	i32 1243150071, ; 180: Xamarin.AndroidX.Window.Extensions.Core.Core.dll => 0x4a18f6f7 => 263
	i32 1246548578, ; 181: Xamarin.AndroidX.Collection.Jvm.dll => 0x4a4cd262 => 208
	i32 1253011324, ; 182: Microsoft.Win32.Registry => 0x4aaf6f7c => 5
	i32 1260983243, ; 183: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 293
	i32 1264511973, ; 184: Xamarin.AndroidX.Startup.StartupRuntime.dll => 0x4b5eebe5 => 253
	i32 1267360935, ; 185: Xamarin.AndroidX.VectorDrawable => 0x4b8a64a7 => 257
	i32 1273260888, ; 186: Xamarin.AndroidX.Collection.Ktx => 0x4be46b58 => 209
	i32 1275534314, ; 187: Xamarin.KotlinX.Coroutines.Android => 0x4c071bea => 288
	i32 1278448581, ; 188: Xamarin.AndroidX.Annotation.Jvm => 0x4c3393c5 => 200
	i32 1293217323, ; 189: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 220
	i32 1309188875, ; 190: System.Private.DataContractSerialization => 0x4e08a30b => 85
	i32 1322716291, ; 191: Xamarin.AndroidX.Window.dll => 0x4ed70c83 => 262
	i32 1324164729, ; 192: System.Linq => 0x4eed2679 => 61
	i32 1333047053, ; 193: Xamarin.Firebase.Common => 0x4f74af0d => 268
	i32 1335329327, ; 194: System.Runtime.Serialization.Json.dll => 0x4f97822f => 112
	i32 1364015309, ; 195: System.IO => 0x514d38cd => 57
	i32 1373134921, ; 196: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 323
	i32 1376866003, ; 197: Xamarin.AndroidX.SavedState => 0x52114ed3 => 249
	i32 1379779777, ; 198: System.Resources.ResourceManager => 0x523dc4c1 => 99
	i32 1379897097, ; 199: Xamarin.JavaX.Inject => 0x523f8f09 => 282
	i32 1402170036, ; 200: System.Configuration.dll => 0x53936ab4 => 19
	i32 1406073936, ; 201: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 213
	i32 1408764838, ; 202: System.Runtime.Serialization.Formatters.dll => 0x53f80ba6 => 111
	i32 1411638395, ; 203: System.Runtime.CompilerServices.Unsafe => 0x5423e47b => 101
	i32 1411702249, ; 204: Xamarin.Firebase.Auth.Interop.dll => 0x5424dde9 => 267
	i32 1422545099, ; 205: System.Runtime.CompilerServices.VisualC => 0x54ca50cb => 102
	i32 1430672901, ; 206: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 291
	i32 1434145427, ; 207: System.Runtime.Handles => 0x557b5293 => 104
	i32 1435222561, ; 208: Xamarin.Google.Crypto.Tink.Android.dll => 0x558bc221 => 275
	i32 1439761251, ; 209: System.Net.Quic.dll => 0x55d10363 => 71
	i32 1452070440, ; 210: System.Formats.Asn1.dll => 0x568cd628 => 38
	i32 1453312822, ; 211: System.Diagnostics.Tools.dll => 0x569fcb36 => 32
	i32 1457743152, ; 212: System.Runtime.Extensions.dll => 0x56e36530 => 103
	i32 1458022317, ; 213: System.Net.Security.dll => 0x56e7a7ad => 73
	i32 1461004990, ; 214: es\Microsoft.Maui.Controls.resources => 0x57152abe => 297
	i32 1461234159, ; 215: System.Collections.Immutable.dll => 0x5718a9ef => 9
	i32 1461719063, ; 216: System.Security.Cryptography.OpenSsl => 0x57201017 => 123
	i32 1462112819, ; 217: System.IO.Compression.dll => 0x57261233 => 46
	i32 1469204771, ; 218: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 202
	i32 1470490898, ; 219: Microsoft.Extensions.Primitives => 0x57a5e912 => 182
	i32 1479771757, ; 220: System.Collections.Immutable => 0x5833866d => 9
	i32 1480492111, ; 221: System.IO.Compression.Brotli.dll => 0x583e844f => 43
	i32 1487239319, ; 222: Microsoft.Win32.Primitives => 0x58a57897 => 4
	i32 1490025113, ; 223: Xamarin.AndroidX.SavedState.SavedState.Ktx.dll => 0x58cffa99 => 250
	i32 1493001747, ; 224: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 301
	i32 1514721132, ; 225: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 296
	i32 1536373174, ; 226: System.Diagnostics.TextWriterTraceListener => 0x5b9331b6 => 31
	i32 1543031311, ; 227: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 138
	i32 1543355203, ; 228: System.Reflection.Emit.dll => 0x5bfdbb43 => 92
	i32 1550322496, ; 229: System.Reflection.Extensions.dll => 0x5c680b40 => 93
	i32 1551623176, ; 230: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 316
	i32 1565862583, ; 231: System.IO.FileSystem.Primitives => 0x5d552ab7 => 49
	i32 1566207040, ; 232: System.Threading.Tasks.Dataflow.dll => 0x5d5a6c40 => 141
	i32 1573704789, ; 233: System.Runtime.Serialization.Json => 0x5dccd455 => 112
	i32 1580037396, ; 234: System.Threading.Overlapped => 0x5e2d7514 => 140
	i32 1582372066, ; 235: Xamarin.AndroidX.DocumentFile.dll => 0x5e5114e2 => 219
	i32 1592978981, ; 236: System.Runtime.Serialization.dll => 0x5ef2ee25 => 115
	i32 1593325549, ; 237: FraudGuardAI => 0x5ef837ed => 0
	i32 1597949149, ; 238: Xamarin.Google.ErrorProne.Annotations => 0x5f3ec4dd => 276
	i32 1601112923, ; 239: System.Xml.Serialization => 0x5f6f0b5b => 157
	i32 1604827217, ; 240: System.Net.WebClient => 0x5fa7b851 => 76
	i32 1618516317, ; 241: System.Net.WebSockets.Client.dll => 0x6078995d => 79
	i32 1622152042, ; 242: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 239
	i32 1622358360, ; 243: System.Dynamic.Runtime => 0x60b33958 => 37
	i32 1624863272, ; 244: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 261
	i32 1635184631, ; 245: Xamarin.AndroidX.Emoji2.ViewsHelper => 0x6176eff7 => 223
	i32 1636350590, ; 246: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 216
	i32 1639515021, ; 247: System.Net.Http.dll => 0x61b9038d => 64
	i32 1639986890, ; 248: System.Text.RegularExpressions => 0x61c036ca => 138
	i32 1641389582, ; 249: System.ComponentModel.EventBasedAsync.dll => 0x61d59e0e => 15
	i32 1657153582, ; 250: System.Runtime => 0x62c6282e => 116
	i32 1658241508, ; 251: Xamarin.AndroidX.Tracing.Tracing.dll => 0x62d6c1e4 => 255
	i32 1658251792, ; 252: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 271
	i32 1670060433, ; 253: Xamarin.AndroidX.ConstraintLayout => 0x638b1991 => 211
	i32 1675553242, ; 254: System.IO.FileSystem.DriveInfo.dll => 0x63dee9da => 48
	i32 1677501392, ; 255: System.Net.Primitives.dll => 0x63fca3d0 => 70
	i32 1678508291, ; 256: System.Net.WebSockets => 0x640c0103 => 80
	i32 1679769178, ; 257: System.Security.Cryptography => 0x641f3e5a => 126
	i32 1691477237, ; 258: System.Reflection.Metadata => 0x64d1e4f5 => 94
	i32 1696967625, ; 259: System.Security.Cryptography.Csp => 0x6525abc9 => 121
	i32 1698840827, ; 260: Xamarin.Kotlin.StdLib.Common => 0x654240fb => 285
	i32 1701541528, ; 261: System.Diagnostics.Debug.dll => 0x656b7698 => 26
	i32 1720223769, ; 262: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx => 0x66888819 => 232
	i32 1726116996, ; 263: System.Reflection.dll => 0x66e27484 => 97
	i32 1728033016, ; 264: System.Diagnostics.FileVersionInfo.dll => 0x66ffb0f8 => 28
	i32 1729485958, ; 265: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 206
	i32 1736233607, ; 266: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 314
	i32 1743415430, ; 267: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 292
	i32 1744735666, ; 268: System.Transactions.Local.dll => 0x67fe8db2 => 149
	i32 1746316138, ; 269: Mono.Android.Export => 0x6816ab6a => 169
	i32 1750313021, ; 270: Microsoft.Win32.Primitives.dll => 0x6853a83d => 4
	i32 1758240030, ; 271: System.Resources.Reader.dll => 0x68cc9d1e => 98
	i32 1763938596, ; 272: System.Diagnostics.TraceSource.dll => 0x69239124 => 33
	i32 1765942094, ; 273: System.Reflection.Extensions => 0x6942234e => 93
	i32 1766324549, ; 274: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 254
	i32 1770582343, ; 275: Microsoft.Extensions.Logging.dll => 0x6988f147 => 178
	i32 1776026572, ; 276: System.Core.dll => 0x69dc03cc => 21
	i32 1777075843, ; 277: System.Globalization.Extensions.dll => 0x69ec0683 => 41
	i32 1780572499, ; 278: Mono.Android.Runtime.dll => 0x6a216153 => 170
	i32 1782862114, ; 279: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 308
	i32 1788241197, ; 280: Xamarin.AndroidX.Fragment => 0x6a96652d => 225
	i32 1793755602, ; 281: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 300
	i32 1808609942, ; 282: Xamarin.AndroidX.Loader => 0x6bcd3296 => 239
	i32 1813058853, ; 283: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 284
	i32 1813201214, ; 284: Xamarin.Google.Android.Material => 0x6c13413e => 271
	i32 1818569960, ; 285: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 244
	i32 1818787751, ; 286: Microsoft.VisualBasic.Core => 0x6c687fa7 => 2
	i32 1824175904, ; 287: System.Text.Encoding.Extensions => 0x6cbab720 => 134
	i32 1824722060, ; 288: System.Runtime.Serialization.Formatters => 0x6cc30c8c => 111
	i32 1828688058, ; 289: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 179
	i32 1842015223, ; 290: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 320
	i32 1847515442, ; 291: Xamarin.Android.Glide.Annotations => 0x6e1ed932 => 193
	i32 1853025655, ; 292: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 317
	i32 1858542181, ; 293: System.Linq.Expressions => 0x6ec71a65 => 58
	i32 1870277092, ; 294: System.Reflection.Primitives => 0x6f7a29e4 => 95
	i32 1875053220, ; 295: Xamarin.Firebase.Auth.Interop => 0x6fc30aa4 => 267
	i32 1875935024, ; 296: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 299
	i32 1879696579, ; 297: System.Formats.Tar.dll => 0x7009e4c3 => 39
	i32 1885316902, ; 298: Xamarin.AndroidX.Arch.Core.Runtime.dll => 0x705fa726 => 204
	i32 1888955245, ; 299: System.Diagnostics.Contracts => 0x70972b6d => 25
	i32 1889954781, ; 300: System.Reflection.Metadata.dll => 0x70a66bdd => 94
	i32 1898237753, ; 301: System.Reflection.DispatchProxy => 0x7124cf39 => 89
	i32 1900610850, ; 302: System.Resources.ResourceManager.dll => 0x71490522 => 99
	i32 1908813208, ; 303: Xamarin.GooglePlayServices.Basement => 0x71c62d98 => 280
	i32 1910275211, ; 304: System.Collections.NonGeneric.dll => 0x71dc7c8b => 10
	i32 1939592360, ; 305: System.Private.Xml.Linq => 0x739bd4a8 => 87
	i32 1943407207, ; 306: Plugin.Firebase.Auth => 0x73d60a67 => 190
	i32 1956758971, ; 307: System.Resources.Writer => 0x74a1c5bb => 100
	i32 1961813231, ; 308: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x74eee4ef => 251
	i32 1968388702, ; 309: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 174
	i32 1983156543, ; 310: Xamarin.Kotlin.StdLib.Common.dll => 0x7634913f => 285
	i32 1985761444, ; 311: Xamarin.Android.Glide.GifDecoder => 0x765c50a4 => 195
	i32 2003115576, ; 312: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 296
	i32 2011961780, ; 313: System.Buffers.dll => 0x77ec19b4 => 7
	i32 2019465201, ; 314: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 236
	i32 2025202353, ; 315: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 291
	i32 2031763787, ; 316: Xamarin.Android.Glide => 0x791a414b => 192
	i32 2045470958, ; 317: System.Private.Xml => 0x79eb68ee => 88
	i32 2055257422, ; 318: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 231
	i32 2060060697, ; 319: System.Windows.dll => 0x7aca0819 => 154
	i32 2066184531, ; 320: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 295
	i32 2070888862, ; 321: System.Diagnostics.TraceSource => 0x7b6f419e => 33
	i32 2079903147, ; 322: System.Runtime.dll => 0x7bf8cdab => 116
	i32 2086218969, ; 323: Xamarin.Google.Android.Play.Integrity.dll => 0x7c592cd9 => 272
	i32 2090596640, ; 324: System.Numerics.Vectors => 0x7c9bf920 => 82
	i32 2127167465, ; 325: System.Console => 0x7ec9ffe9 => 20
	i32 2129483829, ; 326: Xamarin.GooglePlayServices.Base.dll => 0x7eed5835 => 279
	i32 2142473426, ; 327: System.Collections.Specialized => 0x7fb38cd2 => 11
	i32 2143790110, ; 328: System.Xml.XmlSerializer.dll => 0x7fc7a41e => 162
	i32 2146852085, ; 329: Microsoft.VisualBasic.dll => 0x7ff65cf5 => 3
	i32 2159891885, ; 330: Microsoft.Maui => 0x80bd55ad => 186
	i32 2169148018, ; 331: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 303
	i32 2174878672, ; 332: Xamarin.Firebase.Annotations => 0x81a203d0 => 264
	i32 2181898931, ; 333: Microsoft.Extensions.Options.dll => 0x820d22b3 => 181
	i32 2192057212, ; 334: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 179
	i32 2193016926, ; 335: System.ObjectModel.dll => 0x82b6c85e => 84
	i32 2201107256, ; 336: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 289
	i32 2201231467, ; 337: System.Net.Http => 0x8334206b => 64
	i32 2207618523, ; 338: it\Microsoft.Maui.Controls.resources => 0x839595db => 305
	i32 2217644978, ; 339: Xamarin.AndroidX.VectorDrawable.Animated.dll => 0x842e93b2 => 258
	i32 2222056684, ; 340: System.Threading.Tasks.Parallel => 0x8471e4ec => 143
	i32 2225853105, ; 341: Xamarin.Firebase.Common.Ktx => 0x84abd2b1 => 269
	i32 2244775296, ; 342: Xamarin.AndroidX.LocalBroadcastManager => 0x85cc8d80 => 240
	i32 2252106437, ; 343: System.Xml.Serialization.dll => 0x863c6ac5 => 157
	i32 2256313426, ; 344: System.Globalization.Extensions => 0x867c9c52 => 41
	i32 2265110946, ; 345: System.Security.AccessControl.dll => 0x8702d9a2 => 117
	i32 2266799131, ; 346: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 175
	i32 2267999099, ; 347: Xamarin.Android.Glide.DiskLruCache.dll => 0x872eeb7b => 194
	i32 2270573516, ; 348: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 299
	i32 2279755925, ; 349: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 247
	i32 2293034957, ; 350: System.ServiceModel.Web.dll => 0x88acefcd => 131
	i32 2295906218, ; 351: System.Net.Sockets => 0x88d8bfaa => 75
	i32 2298471582, ; 352: System.Net.Mail => 0x88ffe49e => 66
	i32 2303942373, ; 353: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 309
	i32 2305521784, ; 354: System.Private.CoreLib.dll => 0x896b7878 => 172
	i32 2315684594, ; 355: Xamarin.AndroidX.Annotation.dll => 0x8a068af2 => 198
	i32 2320631194, ; 356: System.Threading.Tasks.Parallel.dll => 0x8a52059a => 143
	i32 2340441535, ; 357: System.Runtime.InteropServices.RuntimeInformation.dll => 0x8b804dbf => 106
	i32 2344264397, ; 358: System.ValueTuple => 0x8bbaa2cd => 151
	i32 2353062107, ; 359: System.Net.Primitives => 0x8c40e0db => 70
	i32 2368005991, ; 360: System.Xml.ReaderWriter.dll => 0x8d24e767 => 156
	i32 2371007202, ; 361: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 174
	i32 2378619854, ; 362: System.Security.Cryptography.Csp.dll => 0x8dc6dbce => 121
	i32 2382033717, ; 363: Xamarin.Firebase.Auth.dll => 0x8dfaf335 => 266
	i32 2383496789, ; 364: System.Security.Principal.Windows.dll => 0x8e114655 => 127
	i32 2395872292, ; 365: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 304
	i32 2401565422, ; 366: System.Web.HttpUtility => 0x8f24faee => 152
	i32 2403452196, ; 367: Xamarin.AndroidX.Emoji2.dll => 0x8f41c524 => 222
	i32 2421380589, ; 368: System.Threading.Tasks.Dataflow => 0x905355ed => 141
	i32 2423080555, ; 369: Xamarin.AndroidX.Collection.Ktx.dll => 0x906d466b => 209
	i32 2427813419, ; 370: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 301
	i32 2435356389, ; 371: System.Console.dll => 0x912896e5 => 20
	i32 2435904999, ; 372: System.ComponentModel.DataAnnotations.dll => 0x9130f5e7 => 14
	i32 2454642406, ; 373: System.Text.Encoding.dll => 0x924edee6 => 135
	i32 2458678730, ; 374: System.Net.Sockets.dll => 0x928c75ca => 75
	i32 2459001652, ; 375: System.Linq.Parallel.dll => 0x92916334 => 59
	i32 2465532216, ; 376: Xamarin.AndroidX.ConstraintLayout.Core.dll => 0x92f50938 => 212
	i32 2471841756, ; 377: netstandard.dll => 0x93554fdc => 167
	i32 2475788418, ; 378: Java.Interop.dll => 0x93918882 => 168
	i32 2480646305, ; 379: Microsoft.Maui.Controls => 0x93dba8a1 => 184
	i32 2483903535, ; 380: System.ComponentModel.EventBasedAsync => 0x940d5c2f => 15
	i32 2484371297, ; 381: System.Net.ServicePoint => 0x94147f61 => 74
	i32 2490993605, ; 382: System.AppContext.dll => 0x94798bc5 => 6
	i32 2501346920, ; 383: System.Data.DataSetExtensions => 0x95178668 => 23
	i32 2505896520, ; 384: Xamarin.AndroidX.Lifecycle.Runtime.dll => 0x955cf248 => 234
	i32 2522472828, ; 385: Xamarin.Android.Glide.dll => 0x9659e17c => 192
	i32 2538310050, ; 386: System.Reflection.Emit.Lightweight.dll => 0x974b89a2 => 91
	i32 2550873716, ; 387: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 302
	i32 2561374756, ; 388: Xamarin.Google.Android.Recaptcha.dll => 0x98ab7a24 => 273
	i32 2562349572, ; 389: Microsoft.CSharp => 0x98ba5a04 => 1
	i32 2570120770, ; 390: System.Text.Encodings.Web => 0x9930ee42 => 136
	i32 2581783588, ; 391: Xamarin.AndroidX.Lifecycle.Runtime.Ktx => 0x99e2e424 => 235
	i32 2581819634, ; 392: Xamarin.AndroidX.VectorDrawable.dll => 0x99e370f2 => 257
	i32 2585220780, ; 393: System.Text.Encoding.Extensions.dll => 0x9a1756ac => 134
	i32 2585805581, ; 394: System.Net.Ping => 0x9a20430d => 69
	i32 2589602615, ; 395: System.Threading.ThreadPool => 0x9a5a3337 => 146
	i32 2593496499, ; 396: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 311
	i32 2605712449, ; 397: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 289
	i32 2615233544, ; 398: Xamarin.AndroidX.Fragment.Ktx => 0x9be14c08 => 226
	i32 2616218305, ; 399: Microsoft.Extensions.Logging.Debug.dll => 0x9bf052c1 => 180
	i32 2617129537, ; 400: System.Private.Xml.dll => 0x9bfe3a41 => 88
	i32 2618712057, ; 401: System.Reflection.TypeExtensions.dll => 0x9c165ff9 => 96
	i32 2620871830, ; 402: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 216
	i32 2621637141, ; 403: Plugin.Firebase.Auth.dll => 0x9c430215 => 190
	i32 2624644809, ; 404: Xamarin.AndroidX.DynamicAnimation => 0x9c70e6c9 => 221
	i32 2626831493, ; 405: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 306
	i32 2627185994, ; 406: System.Diagnostics.TextWriterTraceListener.dll => 0x9c97ad4a => 31
	i32 2629843544, ; 407: System.IO.Compression.ZipFile.dll => 0x9cc03a58 => 45
	i32 2633051222, ; 408: Xamarin.AndroidX.Lifecycle.LiveData => 0x9cf12c56 => 230
	i32 2663391936, ; 409: Xamarin.Android.Glide.DiskLruCache => 0x9ec022c0 => 194
	i32 2663698177, ; 410: System.Runtime.Loader => 0x9ec4cf01 => 109
	i32 2664396074, ; 411: System.Xml.XDocument.dll => 0x9ecf752a => 158
	i32 2665622720, ; 412: System.Drawing.Primitives => 0x9ee22cc0 => 35
	i32 2676780864, ; 413: System.Data.Common.dll => 0x9f8c6f40 => 22
	i32 2686887180, ; 414: System.Runtime.Serialization.Xml.dll => 0xa026a50c => 114
	i32 2693849962, ; 415: System.IO.dll => 0xa090e36a => 57
	i32 2701096212, ; 416: Xamarin.AndroidX.Tracing.Tracing => 0xa0ff7514 => 255
	i32 2715334215, ; 417: System.Threading.Tasks.dll => 0xa1d8b647 => 144
	i32 2717744543, ; 418: System.Security.Claims => 0xa1fd7d9f => 118
	i32 2719963679, ; 419: System.Security.Cryptography.Cng.dll => 0xa21f5a1f => 120
	i32 2724373263, ; 420: System.Runtime.Numerics.dll => 0xa262a30f => 110
	i32 2732626843, ; 421: Xamarin.AndroidX.Activity => 0xa2e0939b => 196
	i32 2735172069, ; 422: System.Threading.Channels => 0xa30769e5 => 139
	i32 2737747696, ; 423: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 202
	i32 2740948882, ; 424: System.IO.Pipes.AccessControl => 0xa35f8f92 => 54
	i32 2748088231, ; 425: System.Runtime.InteropServices.JavaScript => 0xa3cc7fa7 => 105
	i32 2752995522, ; 426: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 312
	i32 2758225723, ; 427: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 185
	i32 2764765095, ; 428: Microsoft.Maui.dll => 0xa4caf7a7 => 186
	i32 2765824710, ; 429: System.Text.Encoding.CodePages.dll => 0xa4db22c6 => 133
	i32 2770495804, ; 430: Xamarin.Jetbrains.Annotations.dll => 0xa522693c => 283
	i32 2778768386, ; 431: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 260
	i32 2779977773, ; 432: Xamarin.AndroidX.ResourceInspection.Annotation.dll => 0xa5b3182d => 248
	i32 2785988530, ; 433: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 318
	i32 2788224221, ; 434: Xamarin.AndroidX.Fragment.Ktx.dll => 0xa630ecdd => 226
	i32 2801831435, ; 435: Microsoft.Maui.Graphics => 0xa7008e0b => 188
	i32 2803228030, ; 436: System.Xml.XPath.XDocument.dll => 0xa715dd7e => 159
	i32 2804607052, ; 437: Xamarin.Firebase.Components.dll => 0xa72ae84c => 270
	i32 2806116107, ; 438: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 297
	i32 2810250172, ; 439: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 213
	i32 2819470561, ; 440: System.Xml.dll => 0xa80db4e1 => 163
	i32 2821205001, ; 441: System.ServiceProcess.dll => 0xa8282c09 => 132
	i32 2821294376, ; 442: Xamarin.AndroidX.ResourceInspection.Annotation => 0xa8298928 => 248
	i32 2824502124, ; 443: System.Xml.XmlDocument => 0xa85a7b6c => 161
	i32 2831556043, ; 444: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 310
	i32 2838993487, ; 445: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll => 0xa9379a4f => 237
	i32 2847418871, ; 446: Xamarin.GooglePlayServices.Base => 0xa9b829f7 => 279
	i32 2849599387, ; 447: System.Threading.Overlapped.dll => 0xa9d96f9b => 140
	i32 2853208004, ; 448: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 260
	i32 2855708567, ; 449: Xamarin.AndroidX.Transition => 0xaa36a797 => 256
	i32 2861098320, ; 450: Mono.Android.Export.dll => 0xaa88e550 => 169
	i32 2861189240, ; 451: Microsoft.Maui.Essentials => 0xaa8a4878 => 187
	i32 2870099610, ; 452: Xamarin.AndroidX.Activity.Ktx.dll => 0xab123e9a => 197
	i32 2870458124, ; 453: Xamarin.Firebase.AppCheck.Interop => 0xab17b70c => 265
	i32 2875164099, ; 454: Jsr305Binding.dll => 0xab5f85c3 => 274
	i32 2875220617, ; 455: System.Globalization.Calendars.dll => 0xab606289 => 40
	i32 2884993177, ; 456: Xamarin.AndroidX.ExifInterface => 0xabf58099 => 224
	i32 2887636118, ; 457: System.Net.dll => 0xac1dd496 => 81
	i32 2899753641, ; 458: System.IO.UnmanagedMemoryStream => 0xacd6baa9 => 56
	i32 2900621748, ; 459: System.Dynamic.Runtime.dll => 0xace3f9b4 => 37
	i32 2901442782, ; 460: System.Reflection => 0xacf080de => 97
	i32 2905242038, ; 461: mscorlib.dll => 0xad2a79b6 => 166
	i32 2909740682, ; 462: System.Private.CoreLib => 0xad6f1e8a => 172
	i32 2916838712, ; 463: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 261
	i32 2919462931, ; 464: System.Numerics.Vectors.dll => 0xae037813 => 82
	i32 2921128767, ; 465: Xamarin.AndroidX.Annotation.Experimental.dll => 0xae1ce33f => 199
	i32 2936416060, ; 466: System.Resources.Reader => 0xaf06273c => 98
	i32 2940926066, ; 467: System.Diagnostics.StackTrace.dll => 0xaf4af872 => 30
	i32 2942453041, ; 468: System.Xml.XPath.XDocument => 0xaf624531 => 159
	i32 2959614098, ; 469: System.ComponentModel.dll => 0xb0682092 => 18
	i32 2968338931, ; 470: System.Security.Principal.Windows => 0xb0ed41f3 => 127
	i32 2972252294, ; 471: System.Security.Cryptography.Algorithms.dll => 0xb128f886 => 119
	i32 2978675010, ; 472: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 220
	i32 2987532451, ; 473: Xamarin.AndroidX.Security.SecurityCrypto => 0xb21220a3 => 251
	i32 2996846495, ; 474: Xamarin.AndroidX.Lifecycle.Process.dll => 0xb2a03f9f => 233
	i32 3016983068, ; 475: Xamarin.AndroidX.Startup.StartupRuntime => 0xb3d3821c => 253
	i32 3023353419, ; 476: WindowsBase.dll => 0xb434b64b => 165
	i32 3024354802, ; 477: Xamarin.AndroidX.Legacy.Support.Core.Utils => 0xb443fdf2 => 228
	i32 3038032645, ; 478: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 325
	i32 3056245963, ; 479: Xamarin.AndroidX.SavedState.SavedState.Ktx => 0xb62a9ccb => 250
	i32 3057625584, ; 480: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 241
	i32 3058099980, ; 481: Xamarin.GooglePlayServices.Tasks => 0xb646e70c => 281
	i32 3059408633, ; 482: Mono.Android.Runtime => 0xb65adef9 => 170
	i32 3059793426, ; 483: System.ComponentModel.Primitives => 0xb660be12 => 16
	i32 3071899978, ; 484: Xamarin.Firebase.Common.dll => 0xb719794a => 268
	i32 3075834255, ; 485: System.Threading.Tasks => 0xb755818f => 144
	i32 3077302341, ; 486: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 303
	i32 3090735792, ; 487: System.Security.Cryptography.X509Certificates.dll => 0xb838e2b0 => 125
	i32 3099732863, ; 488: System.Security.Claims.dll => 0xb8c22b7f => 118
	i32 3103600923, ; 489: System.Formats.Asn1 => 0xb8fd311b => 38
	i32 3111772706, ; 490: System.Runtime.Serialization => 0xb979e222 => 115
	i32 3121463068, ; 491: System.IO.FileSystem.AccessControl.dll => 0xba0dbf1c => 47
	i32 3124832203, ; 492: System.Threading.Tasks.Extensions => 0xba4127cb => 142
	i32 3132293585, ; 493: System.Security.AccessControl => 0xbab301d1 => 117
	i32 3147165239, ; 494: System.Diagnostics.Tracing.dll => 0xbb95ee37 => 34
	i32 3148237826, ; 495: GoogleGson.dll => 0xbba64c02 => 173
	i32 3150271759, ; 496: Xamarin.KotlinX.Coroutines.Play.Services.dll => 0xbbc5550f => 290
	i32 3159123045, ; 497: System.Reflection.Primitives.dll => 0xbc4c6465 => 95
	i32 3160747431, ; 498: System.IO.MemoryMappedFiles => 0xbc652da7 => 53
	i32 3178803400, ; 499: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 242
	i32 3192346100, ; 500: System.Security.SecureString => 0xbe4755f4 => 129
	i32 3193515020, ; 501: System.Web => 0xbe592c0c => 153
	i32 3204380047, ; 502: System.Data.dll => 0xbefef58f => 24
	i32 3209718065, ; 503: System.Xml.XmlDocument.dll => 0xbf506931 => 161
	i32 3211777861, ; 504: Xamarin.AndroidX.DocumentFile => 0xbf6fd745 => 219
	i32 3220365878, ; 505: System.Threading => 0xbff2e236 => 148
	i32 3226221578, ; 506: System.Runtime.Handles.dll => 0xc04c3c0a => 104
	i32 3230466174, ; 507: Xamarin.GooglePlayServices.Basement.dll => 0xc08d007e => 280
	i32 3251039220, ; 508: System.Reflection.DispatchProxy.dll => 0xc1c6ebf4 => 89
	i32 3258312781, ; 509: Xamarin.AndroidX.CardView => 0xc235e84d => 206
	i32 3265493905, ; 510: System.Linq.Queryable.dll => 0xc2a37b91 => 60
	i32 3265893370, ; 511: System.Threading.Tasks.Extensions.dll => 0xc2a993fa => 142
	i32 3277815716, ; 512: System.Resources.Writer.dll => 0xc35f7fa4 => 100
	i32 3279906254, ; 513: Microsoft.Win32.Registry.dll => 0xc37f65ce => 5
	i32 3280506390, ; 514: System.ComponentModel.Annotations.dll => 0xc3888e16 => 13
	i32 3290767353, ; 515: System.Security.Cryptography.Encoding => 0xc4251ff9 => 122
	i32 3299363146, ; 516: System.Text.Encoding => 0xc4a8494a => 135
	i32 3303498502, ; 517: System.Diagnostics.FileVersionInfo => 0xc4e76306 => 28
	i32 3305363605, ; 518: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 298
	i32 3316684772, ; 519: System.Net.Requests.dll => 0xc5b097e4 => 72
	i32 3317135071, ; 520: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 217
	i32 3317144872, ; 521: System.Data => 0xc5b79d28 => 24
	i32 3340431453, ; 522: Xamarin.AndroidX.Arch.Core.Runtime => 0xc71af05d => 204
	i32 3345895724, ; 523: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll => 0xc76e512c => 246
	i32 3346324047, ; 524: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 243
	i32 3357674450, ; 525: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 315
	i32 3358260929, ; 526: System.Text.Json => 0xc82afec1 => 137
	i32 3362336904, ; 527: Xamarin.AndroidX.Activity.Ktx => 0xc8693088 => 197
	i32 3362522851, ; 528: Xamarin.AndroidX.Core => 0xc86c06e3 => 214
	i32 3366347497, ; 529: Java.Interop => 0xc8a662e9 => 168
	i32 3374999561, ; 530: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 247
	i32 3381016424, ; 531: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 294
	i32 3395150330, ; 532: System.Runtime.CompilerServices.Unsafe.dll => 0xca5de1fa => 101
	i32 3403906625, ; 533: System.Security.Cryptography.OpenSsl.dll => 0xcae37e41 => 123
	i32 3405233483, ; 534: Xamarin.AndroidX.CustomView.PoolingContainer => 0xcaf7bd4b => 218
	i32 3428513518, ; 535: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 176
	i32 3429136800, ; 536: System.Xml => 0xcc6479a0 => 163
	i32 3430777524, ; 537: netstandard => 0xcc7d82b4 => 167
	i32 3441283291, ; 538: Xamarin.AndroidX.DynamicAnimation.dll => 0xcd1dd0db => 221
	i32 3445260447, ; 539: System.Formats.Tar => 0xcd5a809f => 39
	i32 3452344032, ; 540: Microsoft.Maui.Controls.Compatibility.dll => 0xcdc696e0 => 183
	i32 3463511458, ; 541: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 302
	i32 3471940407, ; 542: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 17
	i32 3476120550, ; 543: Mono.Android => 0xcf3163e6 => 171
	i32 3479583265, ; 544: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 315
	i32 3484440000, ; 545: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 314
	i32 3485117614, ; 546: System.Text.Json.dll => 0xcfbaacae => 137
	i32 3486566296, ; 547: System.Transactions => 0xcfd0c798 => 150
	i32 3493954962, ; 548: Xamarin.AndroidX.Concurrent.Futures.dll => 0xd0418592 => 210
	i32 3509114376, ; 549: System.Xml.Linq => 0xd128d608 => 155
	i32 3515174580, ; 550: System.Security.dll => 0xd1854eb4 => 130
	i32 3530912306, ; 551: System.Configuration => 0xd2757232 => 19
	i32 3539954161, ; 552: System.Net.HttpListener => 0xd2ff69f1 => 65
	i32 3560100363, ; 553: System.Threading.Timer => 0xd432d20b => 147
	i32 3570554715, ; 554: System.IO.FileSystem.AccessControl => 0xd4d2575b => 47
	i32 3580758918, ; 555: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 322
	i32 3597029428, ; 556: Xamarin.Android.Glide.GifDecoder.dll => 0xd6665034 => 195
	i32 3598340787, ; 557: System.Net.WebSockets.Client => 0xd67a52b3 => 79
	i32 3608519521, ; 558: System.Linq.dll => 0xd715a361 => 61
	i32 3624195450, ; 559: System.Runtime.InteropServices.RuntimeInformation => 0xd804d57a => 106
	i32 3627220390, ; 560: Xamarin.AndroidX.Print.dll => 0xd832fda6 => 245
	i32 3633644679, ; 561: Xamarin.AndroidX.Annotation.Experimental => 0xd8950487 => 199
	i32 3638274909, ; 562: System.IO.FileSystem.Primitives.dll => 0xd8dbab5d => 49
	i32 3641597786, ; 563: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 231
	i32 3643446276, ; 564: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 319
	i32 3643854240, ; 565: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 242
	i32 3645089577, ; 566: System.ComponentModel.DataAnnotations => 0xd943a729 => 14
	i32 3657292374, ; 567: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 175
	i32 3660523487, ; 568: System.Net.NetworkInformation => 0xda2f27df => 68
	i32 3672681054, ; 569: Mono.Android.dll => 0xdae8aa5e => 171
	i32 3682565725, ; 570: Xamarin.AndroidX.Browser => 0xdb7f7e5d => 205
	i32 3684561358, ; 571: Xamarin.AndroidX.Concurrent.Futures => 0xdb9df1ce => 210
	i32 3697841164, ; 572: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 324
	i32 3700866549, ; 573: System.Net.WebProxy.dll => 0xdc96bdf5 => 78
	i32 3706696989, ; 574: Xamarin.AndroidX.Core.Core.Ktx.dll => 0xdcefb51d => 215
	i32 3716563718, ; 575: System.Runtime.Intrinsics => 0xdd864306 => 108
	i32 3718780102, ; 576: Xamarin.AndroidX.Annotation => 0xdda814c6 => 198
	i32 3724971120, ; 577: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 241
	i32 3732100267, ; 578: System.Net.NameResolution => 0xde7354ab => 67
	i32 3737834244, ; 579: System.Net.Http.Json.dll => 0xdecad304 => 63
	i32 3748608112, ; 580: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 27
	i32 3751444290, ; 581: System.Xml.XPath => 0xdf9a7f42 => 160
	i32 3786282454, ; 582: Xamarin.AndroidX.Collection => 0xe1ae15d6 => 207
	i32 3792276235, ; 583: System.Collections.NonGeneric => 0xe2098b0b => 10
	i32 3800979733, ; 584: Microsoft.Maui.Controls.Compatibility => 0xe28e5915 => 183
	i32 3802395368, ; 585: System.Collections.Specialized.dll => 0xe2a3f2e8 => 11
	i32 3819260425, ; 586: System.Net.WebProxy => 0xe3a54a09 => 78
	i32 3823082795, ; 587: System.Security.Cryptography.dll => 0xe3df9d2b => 126
	i32 3829621856, ; 588: System.Numerics.dll => 0xe4436460 => 83
	i32 3841636137, ; 589: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 177
	i32 3844307129, ; 590: System.Net.Mail.dll => 0xe52378b9 => 66
	i32 3849253459, ; 591: System.Runtime.InteropServices.dll => 0xe56ef253 => 107
	i32 3870376305, ; 592: System.Net.HttpListener.dll => 0xe6b14171 => 65
	i32 3873536506, ; 593: System.Security.Principal => 0xe6e179fa => 128
	i32 3875112723, ; 594: System.Security.Cryptography.Encoding.dll => 0xe6f98713 => 122
	i32 3885497537, ; 595: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 77
	i32 3885922214, ; 596: Xamarin.AndroidX.Transition.dll => 0xe79e77a6 => 256
	i32 3888767677, ; 597: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller => 0xe7c9e2bd => 246
	i32 3889960447, ; 598: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 323
	i32 3896106733, ; 599: System.Collections.Concurrent.dll => 0xe839deed => 8
	i32 3896760992, ; 600: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 214
	i32 3901907137, ; 601: Microsoft.VisualBasic.Core.dll => 0xe89260c1 => 2
	i32 3910130544, ; 602: Xamarin.AndroidX.Collection.Jvm => 0xe90fdb70 => 208
	i32 3920810846, ; 603: System.IO.Compression.FileSystem.dll => 0xe9b2d35e => 44
	i32 3921031405, ; 604: Xamarin.AndroidX.VersionedParcelable.dll => 0xe9b630ed => 259
	i32 3928044579, ; 605: System.Xml.ReaderWriter => 0xea213423 => 156
	i32 3930554604, ; 606: System.Security.Principal.dll => 0xea4780ec => 128
	i32 3931092270, ; 607: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 244
	i32 3934056515, ; 608: Xamarin.JavaX.Inject.dll => 0xea7cf043 => 282
	i32 3945713374, ; 609: System.Data.DataSetExtensions.dll => 0xeb2ecede => 23
	i32 3953953790, ; 610: System.Text.Encoding.CodePages => 0xebac8bfe => 133
	i32 3955647286, ; 611: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 201
	i32 3959773229, ; 612: Xamarin.AndroidX.Lifecycle.Process => 0xec05582d => 233
	i32 3970018735, ; 613: Xamarin.GooglePlayServices.Tasks.dll => 0xeca1adaf => 281
	i32 3980434154, ; 614: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 318
	i32 3987592930, ; 615: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 300
	i32 4003436829, ; 616: System.Diagnostics.Process.dll => 0xee9f991d => 29
	i32 4015948917, ; 617: Xamarin.AndroidX.Annotation.Jvm.dll => 0xef5e8475 => 200
	i32 4025784931, ; 618: System.Memory => 0xeff49a63 => 62
	i32 4046471985, ; 619: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 185
	i32 4054681211, ; 620: System.Reflection.Emit.ILGeneration => 0xf1ad867b => 90
	i32 4068434129, ; 621: System.Private.Xml.Linq.dll => 0xf27f60d1 => 87
	i32 4073602200, ; 622: System.Threading.dll => 0xf2ce3c98 => 148
	i32 4094352644, ; 623: Microsoft.Maui.Essentials.dll => 0xf40add04 => 187
	i32 4099507663, ; 624: System.Drawing.dll => 0xf45985cf => 36
	i32 4100113165, ; 625: System.Private.Uri => 0xf462c30d => 86
	i32 4101593132, ; 626: Xamarin.AndroidX.Emoji2 => 0xf479582c => 222
	i32 4102112229, ; 627: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 313
	i32 4125707920, ; 628: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 308
	i32 4126470640, ; 629: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 176
	i32 4127667938, ; 630: System.IO.FileSystem.Watcher => 0xf60736e2 => 50
	i32 4130442656, ; 631: System.AppContext => 0xf6318da0 => 6
	i32 4147896353, ; 632: System.Reflection.Emit.ILGeneration.dll => 0xf73be021 => 90
	i32 4150914736, ; 633: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 320
	i32 4151237749, ; 634: System.Core => 0xf76edc75 => 21
	i32 4157403133, ; 635: Xamarin.Firebase.Common.Ktx.dll => 0xf7cceffd => 269
	i32 4159265925, ; 636: System.Xml.XmlSerializer => 0xf7e95c85 => 162
	i32 4161255271, ; 637: System.Reflection.TypeExtensions => 0xf807b767 => 96
	i32 4164802419, ; 638: System.IO.FileSystem.Watcher.dll => 0xf83dd773 => 50
	i32 4181436372, ; 639: System.Runtime.Serialization.Primitives => 0xf93ba7d4 => 113
	i32 4182413190, ; 640: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 238
	i32 4185676441, ; 641: System.Security => 0xf97c5a99 => 130
	i32 4196529839, ; 642: System.Net.WebClient.dll => 0xfa21f6af => 76
	i32 4213026141, ; 643: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 27
	i32 4256097574, ; 644: Xamarin.AndroidX.Core.Core.Ktx => 0xfdaee526 => 215
	i32 4258378803, ; 645: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx => 0xfdd1b433 => 237
	i32 4260525087, ; 646: System.Buffers => 0xfdf2741f => 7
	i32 4271975918, ; 647: Microsoft.Maui.Controls.dll => 0xfea12dee => 184
	i32 4274976490, ; 648: System.Runtime.Numerics => 0xfecef6ea => 110
	i32 4284549794, ; 649: Xamarin.Firebase.Components => 0xff610aa2 => 270
	i32 4292120959, ; 650: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 238
	i32 4294763496 ; 651: Xamarin.AndroidX.ExifInterface.dll => 0xfffce3e8 => 224
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [652 x i32] [
	i32 68, ; 0
	i32 67, ; 1
	i32 189, ; 2
	i32 108, ; 3
	i32 234, ; 4
	i32 277, ; 5
	i32 48, ; 6
	i32 80, ; 7
	i32 145, ; 8
	i32 290, ; 9
	i32 30, ; 10
	i32 324, ; 11
	i32 124, ; 12
	i32 188, ; 13
	i32 102, ; 14
	i32 252, ; 15
	i32 264, ; 16
	i32 107, ; 17
	i32 252, ; 18
	i32 139, ; 19
	i32 286, ; 20
	i32 77, ; 21
	i32 191, ; 22
	i32 124, ; 23
	i32 13, ; 24
	i32 207, ; 25
	i32 132, ; 26
	i32 254, ; 27
	i32 151, ; 28
	i32 321, ; 29
	i32 322, ; 30
	i32 18, ; 31
	i32 205, ; 32
	i32 26, ; 33
	i32 228, ; 34
	i32 1, ; 35
	i32 59, ; 36
	i32 42, ; 37
	i32 91, ; 38
	i32 211, ; 39
	i32 147, ; 40
	i32 230, ; 41
	i32 227, ; 42
	i32 293, ; 43
	i32 54, ; 44
	i32 278, ; 45
	i32 69, ; 46
	i32 321, ; 47
	i32 196, ; 48
	i32 83, ; 49
	i32 306, ; 50
	i32 229, ; 51
	i32 305, ; 52
	i32 131, ; 53
	i32 189, ; 54
	i32 55, ; 55
	i32 149, ; 56
	i32 74, ; 57
	i32 145, ; 58
	i32 62, ; 59
	i32 146, ; 60
	i32 325, ; 61
	i32 165, ; 62
	i32 317, ; 63
	i32 212, ; 64
	i32 12, ; 65
	i32 273, ; 66
	i32 225, ; 67
	i32 125, ; 68
	i32 152, ; 69
	i32 113, ; 70
	i32 166, ; 71
	i32 164, ; 72
	i32 227, ; 73
	i32 240, ; 74
	i32 265, ; 75
	i32 84, ; 76
	i32 304, ; 77
	i32 298, ; 78
	i32 182, ; 79
	i32 150, ; 80
	i32 286, ; 81
	i32 60, ; 82
	i32 178, ; 83
	i32 51, ; 84
	i32 103, ; 85
	i32 114, ; 86
	i32 40, ; 87
	i32 274, ; 88
	i32 263, ; 89
	i32 120, ; 90
	i32 278, ; 91
	i32 312, ; 92
	i32 52, ; 93
	i32 44, ; 94
	i32 119, ; 95
	i32 217, ; 96
	i32 310, ; 97
	i32 223, ; 98
	i32 81, ; 99
	i32 136, ; 100
	i32 259, ; 101
	i32 203, ; 102
	i32 8, ; 103
	i32 73, ; 104
	i32 292, ; 105
	i32 155, ; 106
	i32 288, ; 107
	i32 154, ; 108
	i32 92, ; 109
	i32 283, ; 110
	i32 45, ; 111
	i32 307, ; 112
	i32 295, ; 113
	i32 287, ; 114
	i32 109, ; 115
	i32 129, ; 116
	i32 25, ; 117
	i32 193, ; 118
	i32 72, ; 119
	i32 55, ; 120
	i32 272, ; 121
	i32 46, ; 122
	i32 316, ; 123
	i32 181, ; 124
	i32 218, ; 125
	i32 22, ; 126
	i32 232, ; 127
	i32 86, ; 128
	i32 43, ; 129
	i32 160, ; 130
	i32 71, ; 131
	i32 245, ; 132
	i32 3, ; 133
	i32 42, ; 134
	i32 63, ; 135
	i32 16, ; 136
	i32 53, ; 137
	i32 0, ; 138
	i32 319, ; 139
	i32 277, ; 140
	i32 105, ; 141
	i32 287, ; 142
	i32 191, ; 143
	i32 275, ; 144
	i32 229, ; 145
	i32 34, ; 146
	i32 158, ; 147
	i32 85, ; 148
	i32 32, ; 149
	i32 12, ; 150
	i32 51, ; 151
	i32 56, ; 152
	i32 249, ; 153
	i32 36, ; 154
	i32 177, ; 155
	i32 294, ; 156
	i32 276, ; 157
	i32 201, ; 158
	i32 35, ; 159
	i32 58, ; 160
	i32 236, ; 161
	i32 173, ; 162
	i32 17, ; 163
	i32 284, ; 164
	i32 164, ; 165
	i32 266, ; 166
	i32 307, ; 167
	i32 235, ; 168
	i32 180, ; 169
	i32 262, ; 170
	i32 313, ; 171
	i32 153, ; 172
	i32 258, ; 173
	i32 243, ; 174
	i32 311, ; 175
	i32 203, ; 176
	i32 29, ; 177
	i32 52, ; 178
	i32 309, ; 179
	i32 263, ; 180
	i32 208, ; 181
	i32 5, ; 182
	i32 293, ; 183
	i32 253, ; 184
	i32 257, ; 185
	i32 209, ; 186
	i32 288, ; 187
	i32 200, ; 188
	i32 220, ; 189
	i32 85, ; 190
	i32 262, ; 191
	i32 61, ; 192
	i32 268, ; 193
	i32 112, ; 194
	i32 57, ; 195
	i32 323, ; 196
	i32 249, ; 197
	i32 99, ; 198
	i32 282, ; 199
	i32 19, ; 200
	i32 213, ; 201
	i32 111, ; 202
	i32 101, ; 203
	i32 267, ; 204
	i32 102, ; 205
	i32 291, ; 206
	i32 104, ; 207
	i32 275, ; 208
	i32 71, ; 209
	i32 38, ; 210
	i32 32, ; 211
	i32 103, ; 212
	i32 73, ; 213
	i32 297, ; 214
	i32 9, ; 215
	i32 123, ; 216
	i32 46, ; 217
	i32 202, ; 218
	i32 182, ; 219
	i32 9, ; 220
	i32 43, ; 221
	i32 4, ; 222
	i32 250, ; 223
	i32 301, ; 224
	i32 296, ; 225
	i32 31, ; 226
	i32 138, ; 227
	i32 92, ; 228
	i32 93, ; 229
	i32 316, ; 230
	i32 49, ; 231
	i32 141, ; 232
	i32 112, ; 233
	i32 140, ; 234
	i32 219, ; 235
	i32 115, ; 236
	i32 0, ; 237
	i32 276, ; 238
	i32 157, ; 239
	i32 76, ; 240
	i32 79, ; 241
	i32 239, ; 242
	i32 37, ; 243
	i32 261, ; 244
	i32 223, ; 245
	i32 216, ; 246
	i32 64, ; 247
	i32 138, ; 248
	i32 15, ; 249
	i32 116, ; 250
	i32 255, ; 251
	i32 271, ; 252
	i32 211, ; 253
	i32 48, ; 254
	i32 70, ; 255
	i32 80, ; 256
	i32 126, ; 257
	i32 94, ; 258
	i32 121, ; 259
	i32 285, ; 260
	i32 26, ; 261
	i32 232, ; 262
	i32 97, ; 263
	i32 28, ; 264
	i32 206, ; 265
	i32 314, ; 266
	i32 292, ; 267
	i32 149, ; 268
	i32 169, ; 269
	i32 4, ; 270
	i32 98, ; 271
	i32 33, ; 272
	i32 93, ; 273
	i32 254, ; 274
	i32 178, ; 275
	i32 21, ; 276
	i32 41, ; 277
	i32 170, ; 278
	i32 308, ; 279
	i32 225, ; 280
	i32 300, ; 281
	i32 239, ; 282
	i32 284, ; 283
	i32 271, ; 284
	i32 244, ; 285
	i32 2, ; 286
	i32 134, ; 287
	i32 111, ; 288
	i32 179, ; 289
	i32 320, ; 290
	i32 193, ; 291
	i32 317, ; 292
	i32 58, ; 293
	i32 95, ; 294
	i32 267, ; 295
	i32 299, ; 296
	i32 39, ; 297
	i32 204, ; 298
	i32 25, ; 299
	i32 94, ; 300
	i32 89, ; 301
	i32 99, ; 302
	i32 280, ; 303
	i32 10, ; 304
	i32 87, ; 305
	i32 190, ; 306
	i32 100, ; 307
	i32 251, ; 308
	i32 174, ; 309
	i32 285, ; 310
	i32 195, ; 311
	i32 296, ; 312
	i32 7, ; 313
	i32 236, ; 314
	i32 291, ; 315
	i32 192, ; 316
	i32 88, ; 317
	i32 231, ; 318
	i32 154, ; 319
	i32 295, ; 320
	i32 33, ; 321
	i32 116, ; 322
	i32 272, ; 323
	i32 82, ; 324
	i32 20, ; 325
	i32 279, ; 326
	i32 11, ; 327
	i32 162, ; 328
	i32 3, ; 329
	i32 186, ; 330
	i32 303, ; 331
	i32 264, ; 332
	i32 181, ; 333
	i32 179, ; 334
	i32 84, ; 335
	i32 289, ; 336
	i32 64, ; 337
	i32 305, ; 338
	i32 258, ; 339
	i32 143, ; 340
	i32 269, ; 341
	i32 240, ; 342
	i32 157, ; 343
	i32 41, ; 344
	i32 117, ; 345
	i32 175, ; 346
	i32 194, ; 347
	i32 299, ; 348
	i32 247, ; 349
	i32 131, ; 350
	i32 75, ; 351
	i32 66, ; 352
	i32 309, ; 353
	i32 172, ; 354
	i32 198, ; 355
	i32 143, ; 356
	i32 106, ; 357
	i32 151, ; 358
	i32 70, ; 359
	i32 156, ; 360
	i32 174, ; 361
	i32 121, ; 362
	i32 266, ; 363
	i32 127, ; 364
	i32 304, ; 365
	i32 152, ; 366
	i32 222, ; 367
	i32 141, ; 368
	i32 209, ; 369
	i32 301, ; 370
	i32 20, ; 371
	i32 14, ; 372
	i32 135, ; 373
	i32 75, ; 374
	i32 59, ; 375
	i32 212, ; 376
	i32 167, ; 377
	i32 168, ; 378
	i32 184, ; 379
	i32 15, ; 380
	i32 74, ; 381
	i32 6, ; 382
	i32 23, ; 383
	i32 234, ; 384
	i32 192, ; 385
	i32 91, ; 386
	i32 302, ; 387
	i32 273, ; 388
	i32 1, ; 389
	i32 136, ; 390
	i32 235, ; 391
	i32 257, ; 392
	i32 134, ; 393
	i32 69, ; 394
	i32 146, ; 395
	i32 311, ; 396
	i32 289, ; 397
	i32 226, ; 398
	i32 180, ; 399
	i32 88, ; 400
	i32 96, ; 401
	i32 216, ; 402
	i32 190, ; 403
	i32 221, ; 404
	i32 306, ; 405
	i32 31, ; 406
	i32 45, ; 407
	i32 230, ; 408
	i32 194, ; 409
	i32 109, ; 410
	i32 158, ; 411
	i32 35, ; 412
	i32 22, ; 413
	i32 114, ; 414
	i32 57, ; 415
	i32 255, ; 416
	i32 144, ; 417
	i32 118, ; 418
	i32 120, ; 419
	i32 110, ; 420
	i32 196, ; 421
	i32 139, ; 422
	i32 202, ; 423
	i32 54, ; 424
	i32 105, ; 425
	i32 312, ; 426
	i32 185, ; 427
	i32 186, ; 428
	i32 133, ; 429
	i32 283, ; 430
	i32 260, ; 431
	i32 248, ; 432
	i32 318, ; 433
	i32 226, ; 434
	i32 188, ; 435
	i32 159, ; 436
	i32 270, ; 437
	i32 297, ; 438
	i32 213, ; 439
	i32 163, ; 440
	i32 132, ; 441
	i32 248, ; 442
	i32 161, ; 443
	i32 310, ; 444
	i32 237, ; 445
	i32 279, ; 446
	i32 140, ; 447
	i32 260, ; 448
	i32 256, ; 449
	i32 169, ; 450
	i32 187, ; 451
	i32 197, ; 452
	i32 265, ; 453
	i32 274, ; 454
	i32 40, ; 455
	i32 224, ; 456
	i32 81, ; 457
	i32 56, ; 458
	i32 37, ; 459
	i32 97, ; 460
	i32 166, ; 461
	i32 172, ; 462
	i32 261, ; 463
	i32 82, ; 464
	i32 199, ; 465
	i32 98, ; 466
	i32 30, ; 467
	i32 159, ; 468
	i32 18, ; 469
	i32 127, ; 470
	i32 119, ; 471
	i32 220, ; 472
	i32 251, ; 473
	i32 233, ; 474
	i32 253, ; 475
	i32 165, ; 476
	i32 228, ; 477
	i32 325, ; 478
	i32 250, ; 479
	i32 241, ; 480
	i32 281, ; 481
	i32 170, ; 482
	i32 16, ; 483
	i32 268, ; 484
	i32 144, ; 485
	i32 303, ; 486
	i32 125, ; 487
	i32 118, ; 488
	i32 38, ; 489
	i32 115, ; 490
	i32 47, ; 491
	i32 142, ; 492
	i32 117, ; 493
	i32 34, ; 494
	i32 173, ; 495
	i32 290, ; 496
	i32 95, ; 497
	i32 53, ; 498
	i32 242, ; 499
	i32 129, ; 500
	i32 153, ; 501
	i32 24, ; 502
	i32 161, ; 503
	i32 219, ; 504
	i32 148, ; 505
	i32 104, ; 506
	i32 280, ; 507
	i32 89, ; 508
	i32 206, ; 509
	i32 60, ; 510
	i32 142, ; 511
	i32 100, ; 512
	i32 5, ; 513
	i32 13, ; 514
	i32 122, ; 515
	i32 135, ; 516
	i32 28, ; 517
	i32 298, ; 518
	i32 72, ; 519
	i32 217, ; 520
	i32 24, ; 521
	i32 204, ; 522
	i32 246, ; 523
	i32 243, ; 524
	i32 315, ; 525
	i32 137, ; 526
	i32 197, ; 527
	i32 214, ; 528
	i32 168, ; 529
	i32 247, ; 530
	i32 294, ; 531
	i32 101, ; 532
	i32 123, ; 533
	i32 218, ; 534
	i32 176, ; 535
	i32 163, ; 536
	i32 167, ; 537
	i32 221, ; 538
	i32 39, ; 539
	i32 183, ; 540
	i32 302, ; 541
	i32 17, ; 542
	i32 171, ; 543
	i32 315, ; 544
	i32 314, ; 545
	i32 137, ; 546
	i32 150, ; 547
	i32 210, ; 548
	i32 155, ; 549
	i32 130, ; 550
	i32 19, ; 551
	i32 65, ; 552
	i32 147, ; 553
	i32 47, ; 554
	i32 322, ; 555
	i32 195, ; 556
	i32 79, ; 557
	i32 61, ; 558
	i32 106, ; 559
	i32 245, ; 560
	i32 199, ; 561
	i32 49, ; 562
	i32 231, ; 563
	i32 319, ; 564
	i32 242, ; 565
	i32 14, ; 566
	i32 175, ; 567
	i32 68, ; 568
	i32 171, ; 569
	i32 205, ; 570
	i32 210, ; 571
	i32 324, ; 572
	i32 78, ; 573
	i32 215, ; 574
	i32 108, ; 575
	i32 198, ; 576
	i32 241, ; 577
	i32 67, ; 578
	i32 63, ; 579
	i32 27, ; 580
	i32 160, ; 581
	i32 207, ; 582
	i32 10, ; 583
	i32 183, ; 584
	i32 11, ; 585
	i32 78, ; 586
	i32 126, ; 587
	i32 83, ; 588
	i32 177, ; 589
	i32 66, ; 590
	i32 107, ; 591
	i32 65, ; 592
	i32 128, ; 593
	i32 122, ; 594
	i32 77, ; 595
	i32 256, ; 596
	i32 246, ; 597
	i32 323, ; 598
	i32 8, ; 599
	i32 214, ; 600
	i32 2, ; 601
	i32 208, ; 602
	i32 44, ; 603
	i32 259, ; 604
	i32 156, ; 605
	i32 128, ; 606
	i32 244, ; 607
	i32 282, ; 608
	i32 23, ; 609
	i32 133, ; 610
	i32 201, ; 611
	i32 233, ; 612
	i32 281, ; 613
	i32 318, ; 614
	i32 300, ; 615
	i32 29, ; 616
	i32 200, ; 617
	i32 62, ; 618
	i32 185, ; 619
	i32 90, ; 620
	i32 87, ; 621
	i32 148, ; 622
	i32 187, ; 623
	i32 36, ; 624
	i32 86, ; 625
	i32 222, ; 626
	i32 313, ; 627
	i32 308, ; 628
	i32 176, ; 629
	i32 50, ; 630
	i32 6, ; 631
	i32 90, ; 632
	i32 320, ; 633
	i32 21, ; 634
	i32 269, ; 635
	i32 162, ; 636
	i32 96, ; 637
	i32 50, ; 638
	i32 113, ; 639
	i32 238, ; 640
	i32 130, ; 641
	i32 76, ; 642
	i32 27, ; 643
	i32 215, ; 644
	i32 237, ; 645
	i32 7, ; 646
	i32 184, ; 647
	i32 110, ; 648
	i32 270, ; 649
	i32 238, ; 650
	i32 224 ; 651
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
