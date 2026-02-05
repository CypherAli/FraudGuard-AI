; ModuleID = 'marshal_methods.x86_64.ll'
source_filename = "marshal_methods.x86_64.ll"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [136 x ptr] zeroinitializer, align 16

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [272 x i64] [
	i64 98382396393917666, ; 0: Microsoft.Extensions.Primitives.dll => 0x15d8644ad360ce2 => 42
	i64 120698629574877762, ; 1: Mono.Android => 0x1accec39cafe242 => 135
	i64 131669012237370309, ; 2: Microsoft.Maui.Essentials.dll => 0x1d3c844de55c3c5 => 46
	i64 196720943101637631, ; 3: System.Linq.Expressions.dll => 0x2bae4a7cd73f3ff => 100
	i64 232391251801502327, ; 4: Xamarin.AndroidX.SavedState.dll => 0x3399e9cbc897277 => 72
	i64 233177144301842968, ; 5: Xamarin.AndroidX.Collection.Jvm.dll => 0x33c696097d9f218 => 55
	i64 435118502366263740, ; 6: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x609d9f8f8bdb9bc => 73
	i64 545109961164950392, ; 7: fi/Microsoft.Maui.Controls.resources.dll => 0x7909e9f1ec38b78 => 7
	i64 750875890346172408, ; 8: System.Threading.Thread => 0xa6ba5a4da7d1ff8 => 127
	i64 752551065888543710, ; 9: Plugin.AudioRecorder => 0xa7199350da0d3de => 48
	i64 799765834175365804, ; 10: System.ComponentModel.dll => 0xb1956c9f18442ac => 94
	i64 849051935479314978, ; 11: hi/Microsoft.Maui.Controls.resources.dll => 0xbc8703ca21a3a22 => 10
	i64 872800313462103108, ; 12: Xamarin.AndroidX.DrawerLayout => 0xc1ccf42c3c21c44 => 60
	i64 1120440138749646132, ; 13: Xamarin.Google.Android.Material.dll => 0xf8c9a5eae431534 => 82
	i64 1121665720830085036, ; 14: nb/Microsoft.Maui.Controls.resources.dll => 0xf90f507becf47ac => 18
	i64 1369545283391376210, ; 15: Xamarin.AndroidX.Navigation.Fragment.dll => 0x13019a2dd85acb52 => 68
	i64 1392315331768750440, ; 16: Xamarin.Firebase.Auth.Interop.dll => 0x13527f6add681168 => 79
	i64 1465843056802068477, ; 17: Xamarin.Firebase.Components.dll => 0x1457b87e6928f7fd => 81
	i64 1476839205573959279, ; 18: System.Net.Primitives.dll => 0x147ec96ece9b1e6f => 107
	i64 1486715745332614827, ; 19: Microsoft.Maui.Controls.dll => 0x14a1e017ea87d6ab => 43
	i64 1513467482682125403, ; 20: Mono.Android.Runtime => 0x1500eaa8245f6c5b => 134
	i64 1537168428375924959, ; 21: System.Threading.Thread.dll => 0x15551e8a954ae0df => 127
	i64 1556147632182429976, ; 22: ko/Microsoft.Maui.Controls.resources.dll => 0x15988c06d24c8918 => 16
	i64 1624659445732251991, ; 23: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0x168bf32877da9957 => 53
	i64 1628611045998245443, ; 24: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0x1699fd1e1a00b643 => 65
	i64 1743969030606105336, ; 25: System.Memory.dll => 0x1833d297e88f2af8 => 102
	i64 1767386781656293639, ; 26: System.Private.Uri.dll => 0x188704e9f5582107 => 116
	i64 1795316252682057001, ; 27: Xamarin.AndroidX.AppCompat.dll => 0x18ea3e9eac997529 => 52
	i64 1835311033149317475, ; 28: es\Microsoft.Maui.Controls.resources => 0x197855a927386163 => 6
	i64 1836611346387731153, ; 29: Xamarin.AndroidX.SavedState => 0x197cf449ebe482d1 => 72
	i64 1881198190668717030, ; 30: tr\Microsoft.Maui.Controls.resources => 0x1a1b5bc992ea9be6 => 28
	i64 1897575647115118287, ; 31: Xamarin.AndroidX.Security.SecurityCrypto => 0x1a558aff4cba86cf => 73
	i64 1920760634179481754, ; 32: Microsoft.Maui.Controls.Xaml => 0x1aa7e99ec2d2709a => 44
	i64 1959996714666907089, ; 33: tr/Microsoft.Maui.Controls.resources.dll => 0x1b334ea0a2a755d1 => 28
	i64 1981742497975770890, ; 34: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x1b80904d5c241f0a => 64
	i64 1983698669889758782, ; 35: cs/Microsoft.Maui.Controls.resources.dll => 0x1b87836e2031a63e => 2
	i64 2019660174692588140, ; 36: pl/Microsoft.Maui.Controls.resources.dll => 0x1c07463a6f8e1a6c => 20
	i64 2262844636196693701, ; 37: Xamarin.AndroidX.DrawerLayout.dll => 0x1f673d352266e6c5 => 60
	i64 2287834202362508563, ; 38: System.Collections.Concurrent => 0x1fc00515e8ce7513 => 88
	i64 2302323944321350744, ; 39: ru/Microsoft.Maui.Controls.resources.dll => 0x1ff37f6ddb267c58 => 24
	i64 2329709569556905518, ; 40: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x2054ca829b447e2e => 63
	i64 2335503487726329082, ; 41: System.Text.Encodings.Web => 0x2069600c4d9d1cfa => 123
	i64 2470498323731680442, ; 42: Xamarin.AndroidX.CoordinatorLayout => 0x2248f922dc398cba => 56
	i64 2497223385847772520, ; 43: System.Runtime => 0x22a7eb7046413568 => 121
	i64 2547086958574651984, ; 44: Xamarin.AndroidX.Activity.dll => 0x2359121801df4a50 => 51
	i64 2600567737329075491, ; 45: FraudGuardAI.dll => 0x2417129425d74523 => 87
	i64 2602673633151553063, ; 46: th\Microsoft.Maui.Controls.resources => 0x241e8de13a460e27 => 27
	i64 2624866290265602282, ; 47: mscorlib.dll => 0x246d65fbde2db8ea => 131
	i64 2632269733008246987, ; 48: System.Net.NameResolution => 0x2487b36034f808cb => 105
	i64 2656907746661064104, ; 49: Microsoft.Extensions.DependencyInjection => 0x24df3b84c8b75da8 => 37
	i64 2662981627730767622, ; 50: cs\Microsoft.Maui.Controls.resources => 0x24f4cfae6c48af06 => 2
	i64 2706075432581334785, ; 51: System.Net.WebSockets => 0x258de944be6c0701 => 113
	i64 2895129759130297543, ; 52: fi\Microsoft.Maui.Controls.resources => 0x282d912d479fa4c7 => 7
	i64 3017704767998173186, ; 53: Xamarin.Google.Android.Material => 0x29e10a7f7d88a002 => 82
	i64 3289520064315143713, ; 54: Xamarin.AndroidX.Lifecycle.Common => 0x2da6b911e3063621 => 62
	i64 3311221304742556517, ; 55: System.Numerics.Vectors.dll => 0x2df3d23ba9e2b365 => 114
	i64 3325875462027654285, ; 56: System.Runtime.Numerics => 0x2e27e21c8958b48d => 120
	i64 3328853167529574890, ; 57: System.Net.Sockets.dll => 0x2e327651a008c1ea => 110
	i64 3344514922410554693, ; 58: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x2e6a1a9a18463545 => 86
	i64 3364695309916733813, ; 59: Xamarin.Firebase.Common => 0x2eb1cc8eb5028175 => 80
	i64 3411255996856937470, ; 60: Xamarin.GooglePlayServices.Basement => 0x2f5737416a942bfe => 83
	i64 3429672777697402584, ; 61: Microsoft.Maui.Essentials => 0x2f98a5385a7b1ed8 => 46
	i64 3494946837667399002, ; 62: Microsoft.Extensions.Configuration => 0x30808ba1c00a455a => 35
	i64 3522470458906976663, ; 63: Xamarin.AndroidX.SwipeRefreshLayout => 0x30e2543832f52197 => 74
	i64 3551103847008531295, ; 64: System.Private.CoreLib.dll => 0x31480e226177735f => 132
	i64 3567343442040498961, ; 65: pt\Microsoft.Maui.Controls.resources => 0x3181bff5bea4ab11 => 22
	i64 3571415421602489686, ; 66: System.Runtime.dll => 0x319037675df7e556 => 121
	i64 3638003163729360188, ; 67: Microsoft.Extensions.Configuration.Abstractions => 0x327cc89a39d5f53c => 36
	i64 3647754201059316852, ; 68: System.Xml.ReaderWriter => 0x329f6d1e86145474 => 129
	i64 3655542548057982301, ; 69: Microsoft.Extensions.Configuration.dll => 0x32bb18945e52855d => 35
	i64 3727469159507183293, ; 70: Xamarin.AndroidX.RecyclerView => 0x33baa1739ba646bd => 71
	i64 3869221888984012293, ; 71: Microsoft.Extensions.Logging.dll => 0x35b23cceda0ed605 => 39
	i64 3890352374528606784, ; 72: Microsoft.Maui.Controls.Xaml.dll => 0x35fd4edf66e00240 => 44
	i64 3933965368022646939, ; 73: System.Net.Requests => 0x369840a8bfadc09b => 108
	i64 3966267475168208030, ; 74: System.Memory => 0x370b03412596249e => 102
	i64 4073500526318903918, ; 75: System.Private.Xml.dll => 0x3887fb25779ae26e => 117
	i64 4120493066591692148, ; 76: zh-Hant\Microsoft.Maui.Controls.resources => 0x392eee9cdda86574 => 33
	i64 4154383907710350974, ; 77: System.ComponentModel => 0x39a7562737acb67e => 94
	i64 4187479170553454871, ; 78: System.Linq.Expressions => 0x3a1cea1e912fa117 => 100
	i64 4205801962323029395, ; 79: System.ComponentModel.TypeConverter => 0x3a5e0299f7e7ad93 => 93
	i64 4247996603072512073, ; 80: Xamarin.GooglePlayServices.Tasks => 0x3af3ea6755340049 => 84
	i64 4356591372459378815, ; 81: vi/Microsoft.Maui.Controls.resources.dll => 0x3c75b8c562f9087f => 30
	i64 4636684751163556186, ; 82: Xamarin.AndroidX.VersionedParcelable.dll => 0x4058d0370893015a => 75
	i64 4679594760078841447, ; 83: ar/Microsoft.Maui.Controls.resources.dll => 0x40f142a407475667 => 0
	i64 4702770163853758138, ; 84: Xamarin.Firebase.Components => 0x4143988c34cf0eba => 81
	i64 4794310189461587505, ; 85: Xamarin.AndroidX.Activity => 0x4288cfb749e4c631 => 51
	i64 4795410492532947900, ; 86: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0x428cb86f8f9b7bbc => 74
	i64 4814660307502931973, ; 87: System.Net.NameResolution.dll => 0x42d11c0a5ee2a005 => 105
	i64 4853321196694829351, ; 88: System.Runtime.Loader.dll => 0x435a75ea15de7927 => 119
	i64 5103417709280584325, ; 89: System.Collections.Specialized => 0x46d2fb5e161b6285 => 90
	i64 5182934613077526976, ; 90: System.Collections.Specialized.dll => 0x47ed7b91fa9009c0 => 90
	i64 5290786973231294105, ; 91: System.Runtime.Loader => 0x496ca6b869b72699 => 119
	i64 5426193594926737925, ; 92: Plugin.Firebase.Core => 0x4b4db6534c1baa05 => 50
	i64 5471532531798518949, ; 93: sv\Microsoft.Maui.Controls.resources => 0x4beec9d926d82ca5 => 26
	i64 5522859530602327440, ; 94: uk\Microsoft.Maui.Controls.resources => 0x4ca5237b51eead90 => 29
	i64 5570799893513421663, ; 95: System.IO.Compression.Brotli => 0x4d4f74fcdfa6c35f => 98
	i64 5573260873512690141, ; 96: System.Security.Cryptography.dll => 0x4d58333c6e4ea1dd => 122
	i64 5692067934154308417, ; 97: Xamarin.AndroidX.ViewPager2.dll => 0x4efe49a0d4a8bb41 => 77
	i64 5979151488806146654, ; 98: System.Formats.Asn1 => 0x52fa3699a489d25e => 97
	i64 6068057819846744445, ; 99: ro/Microsoft.Maui.Controls.resources.dll => 0x5436126fec7f197d => 23
	i64 6200764641006662125, ; 100: ro\Microsoft.Maui.Controls.resources => 0x560d8a96830131ed => 23
	i64 6222399776351216807, ; 101: System.Text.Json.dll => 0x565a67a0ffe264a7 => 124
	i64 6357457916754632952, ; 102: _Microsoft.Android.Resource.Designer => 0x583a3a4ac2a7a0f8 => 34
	i64 6401687960814735282, ; 103: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0x58d75d486341cfb2 => 63
	i64 6403742896930319886, ; 104: Xamarin.Firebase.Auth.dll => 0x58deaa3c7c766e0e => 78
	i64 6478287442656530074, ; 105: hr\Microsoft.Maui.Controls.resources => 0x59e7801b0c6a8e9a => 11
	i64 6548213210057960872, ; 106: Xamarin.AndroidX.CustomView.dll => 0x5adfed387b066da8 => 59
	i64 6560151584539558821, ; 107: Microsoft.Extensions.Options => 0x5b0a571be53243a5 => 41
	i64 6743165466166707109, ; 108: nl\Microsoft.Maui.Controls.resources => 0x5d948943c08c43a5 => 19
	i64 6777482997383978746, ; 109: pt/Microsoft.Maui.Controls.resources.dll => 0x5e0e74e0a2525efa => 22
	i64 6894844156784520562, ; 110: System.Numerics.Vectors => 0x5faf683aead1ad72 => 114
	i64 7220009545223068405, ; 111: sv/Microsoft.Maui.Controls.resources.dll => 0x6432a06d99f35af5 => 26
	i64 7270811800166795866, ; 112: System.Linq => 0x64e71ccf51a90a5a => 101
	i64 7377312882064240630, ; 113: System.ComponentModel.TypeConverter.dll => 0x66617afac45a2ff6 => 93
	i64 7489048572193775167, ; 114: System.ObjectModel => 0x67ee71ff6b419e3f => 115
	i64 7543883384044312029, ; 115: FraudGuardAI => 0x68b141f7f4eed5dd => 87
	i64 7586519340370716875, ; 116: Plugin.Firebase.Auth => 0x6948bb251c6d68cb => 49
	i64 7654504624184590948, ; 117: System.Net.Http => 0x6a3a4366801b8264 => 104
	i64 7708790323521193081, ; 118: ms/Microsoft.Maui.Controls.resources.dll => 0x6afb1ff4d1730479 => 17
	i64 7714652370974252055, ; 119: System.Private.CoreLib => 0x6b0ff375198b9c17 => 132
	i64 7735352534559001595, ; 120: Xamarin.Kotlin.StdLib.dll => 0x6b597e2582ce8bfb => 85
	i64 7836164640616011524, ; 121: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x6cbfa6390d64d704 => 53
	i64 8064050204834738623, ; 122: System.Collections.dll => 0x6fe942efa61731bf => 91
	i64 8083354569033831015, ; 123: Xamarin.AndroidX.Lifecycle.Common.dll => 0x702dd82730cad267 => 62
	i64 8085230611270010360, ; 124: System.Net.Http.Json.dll => 0x703482674fdd05f8 => 103
	i64 8087206902342787202, ; 125: System.Diagnostics.DiagnosticSource => 0x703b87d46f3aa082 => 96
	i64 8167236081217502503, ; 126: Java.Interop.dll => 0x7157d9f1a9b8fd27 => 133
	i64 8185542183669246576, ; 127: System.Collections => 0x7198e33f4794aa70 => 91
	i64 8246048515196606205, ; 128: Microsoft.Maui.Graphics.dll => 0x726fd96f64ee56fd => 47
	i64 8368701292315763008, ; 129: System.Security.Cryptography => 0x7423997c6fd56140 => 122
	i64 8400357532724379117, ; 130: Xamarin.AndroidX.Navigation.UI.dll => 0x749410ab44503ded => 70
	i64 8563666267364444763, ; 131: System.Private.Uri => 0x76d841191140ca5b => 116
	i64 8614108721271900878, ; 132: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x778b763e14018ace => 21
	i64 8626175481042262068, ; 133: Java.Interop => 0x77b654e585b55834 => 133
	i64 8639588376636138208, ; 134: Xamarin.AndroidX.Navigation.Runtime => 0x77e5fbdaa2fda2e0 => 69
	i64 8677882282824630478, ; 135: pt-BR\Microsoft.Maui.Controls.resources => 0x786e07f5766b00ce => 21
	i64 8725526185868997716, ; 136: System.Diagnostics.DiagnosticSource.dll => 0x79174bd613173454 => 96
	i64 9045785047181495996, ; 137: zh-HK\Microsoft.Maui.Controls.resources => 0x7d891592e3cb0ebc => 31
	i64 9285318971778582014, ; 138: Plugin.Firebase.Core.dll => 0x80dc1468bb0ec5fe => 50
	i64 9312692141327339315, ; 139: Xamarin.AndroidX.ViewPager2 => 0x813d54296a634f33 => 77
	i64 9324707631942237306, ; 140: Xamarin.AndroidX.AppCompat => 0x8168042fd44a7c7a => 52
	i64 9659729154652888475, ; 141: System.Text.RegularExpressions => 0x860e407c9991dd9b => 125
	i64 9678050649315576968, ; 142: Xamarin.AndroidX.CoordinatorLayout.dll => 0x864f57c9feb18c88 => 56
	i64 9702891218465930390, ; 143: System.Collections.NonGeneric.dll => 0x86a79827b2eb3c96 => 89
	i64 9808709177481450983, ; 144: Mono.Android.dll => 0x881f890734e555e7 => 135
	i64 9956195530459977388, ; 145: Microsoft.Maui => 0x8a2b8315b36616ac => 45
	i64 9991543690424095600, ; 146: es/Microsoft.Maui.Controls.resources.dll => 0x8aa9180c89861370 => 6
	i64 10038780035334861115, ; 147: System.Net.Http.dll => 0x8b50e941206af13b => 104
	i64 10051358222726253779, ; 148: System.Private.Xml => 0x8b7d990c97ccccd3 => 117
	i64 10078727084704864206, ; 149: System.Net.WebSockets.Client => 0x8bded4e257f117ce => 112
	i64 10092835686693276772, ; 150: Microsoft.Maui.Controls => 0x8c10f49539bd0c64 => 43
	i64 10143853363526200146, ; 151: da\Microsoft.Maui.Controls.resources => 0x8cc634e3c2a16b52 => 3
	i64 10229024438826829339, ; 152: Xamarin.AndroidX.CustomView => 0x8df4cb880b10061b => 59
	i64 10406448008575299332, ; 153: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x906b2153fcb3af04 => 86
	i64 10430153318873392755, ; 154: Xamarin.AndroidX.Core => 0x90bf592ea44f6673 => 57
	i64 10506226065143327199, ; 155: ca\Microsoft.Maui.Controls.resources => 0x91cd9cf11ed169df => 1
	i64 10785150219063592792, ; 156: System.Net.Primitives => 0x95ac8cfb68830758 => 107
	i64 10842631359359123634, ; 157: Plugin.Firebase.Auth.dll => 0x9678c3c64d1c80b2 => 49
	i64 11002576679268595294, ; 158: Microsoft.Extensions.Logging.Abstractions => 0x98b1013215cd365e => 40
	i64 11009005086950030778, ; 159: Microsoft.Maui.dll => 0x98c7d7cc621ffdba => 45
	i64 11103970607964515343, ; 160: hu\Microsoft.Maui.Controls.resources => 0x9a193a6fc41a6c0f => 12
	i64 11162124722117608902, ; 161: Xamarin.AndroidX.ViewPager => 0x9ae7d54b986d05c6 => 76
	i64 11220793807500858938, ; 162: ja\Microsoft.Maui.Controls.resources => 0x9bb8448481fdd63a => 15
	i64 11226290749488709958, ; 163: Microsoft.Extensions.Options.dll => 0x9bcbcbf50c874146 => 41
	i64 11299661109949763898, ; 164: Xamarin.AndroidX.Collection.Jvm => 0x9cd075e94cda113a => 55
	i64 11340910727871153756, ; 165: Xamarin.AndroidX.CursorAdapter => 0x9d630238642d465c => 58
	i64 11485890710487134646, ; 166: System.Runtime.InteropServices => 0x9f6614bf0f8b71b6 => 118
	i64 11518296021396496455, ; 167: id\Microsoft.Maui.Controls.resources => 0x9fd9353475222047 => 13
	i64 11529969570048099689, ; 168: Xamarin.AndroidX.ViewPager.dll => 0xa002ae3c4dc7c569 => 76
	i64 11530571088791430846, ; 169: Microsoft.Extensions.Logging => 0xa004d1504ccd66be => 39
	i64 11705530742807338875, ; 170: he/Microsoft.Maui.Controls.resources.dll => 0xa272663128721f7b => 9
	i64 11999166035137088330, ; 171: Plugin.AudioRecorder.dll => 0xa68599ef45474b4a => 48
	i64 12145679461940342714, ; 172: System.Text.Json => 0xa88e1f1ebcb62fba => 124
	i64 12451044538927396471, ; 173: Xamarin.AndroidX.Fragment.dll => 0xaccaff0a2955b677 => 61
	i64 12466513435562512481, ; 174: Xamarin.AndroidX.Loader.dll => 0xad01f3eb52569061 => 66
	i64 12475113361194491050, ; 175: _Microsoft.Android.Resource.Designer.dll => 0xad2081818aba1caa => 34
	i64 12538491095302438457, ; 176: Xamarin.AndroidX.CardView.dll => 0xae01ab382ae67e39 => 54
	i64 12550732019250633519, ; 177: System.IO.Compression => 0xae2d28465e8e1b2f => 99
	i64 12681088699309157496, ; 178: it/Microsoft.Maui.Controls.resources.dll => 0xaffc46fc178aec78 => 14
	i64 12823819093633476069, ; 179: th/Microsoft.Maui.Controls.resources.dll => 0xb1f75b85abe525e5 => 27
	i64 12843321153144804894, ; 180: Microsoft.Extensions.Primitives => 0xb23ca48abd74d61e => 42
	i64 12859557719246324186, ; 181: System.Net.WebHeaderCollection.dll => 0xb276539ce04f41da => 111
	i64 13221551921002590604, ; 182: ca/Microsoft.Maui.Controls.resources.dll => 0xb77c636bdebe318c => 1
	i64 13222659110913276082, ; 183: ja/Microsoft.Maui.Controls.resources.dll => 0xb78052679c1178b2 => 15
	i64 13343850469010654401, ; 184: Mono.Android.Runtime.dll => 0xb92ee14d854f44c1 => 134
	i64 13381594904270902445, ; 185: he\Microsoft.Maui.Controls.resources => 0xb9b4f9aaad3e94ad => 9
	i64 13465488254036897740, ; 186: Xamarin.Kotlin.StdLib => 0xbadf06394d106fcc => 85
	i64 13467053111158216594, ; 187: uk/Microsoft.Maui.Controls.resources.dll => 0xbae49573fde79792 => 29
	i64 13540124433173649601, ; 188: vi\Microsoft.Maui.Controls.resources => 0xbbe82f6eede718c1 => 30
	i64 13545416393490209236, ; 189: id/Microsoft.Maui.Controls.resources.dll => 0xbbfafc7174bc99d4 => 13
	i64 13572454107664307259, ; 190: Xamarin.AndroidX.RecyclerView.dll => 0xbc5b0b19d99f543b => 71
	i64 13717397318615465333, ; 191: System.ComponentModel.Primitives.dll => 0xbe5dfc2ef2f87d75 => 92
	i64 13755568601956062840, ; 192: fr/Microsoft.Maui.Controls.resources.dll => 0xbee598c36b1b9678 => 8
	i64 13814445057219246765, ; 193: hr/Microsoft.Maui.Controls.resources.dll => 0xbfb6c49664b43aad => 11
	i64 13881769479078963060, ; 194: System.Console.dll => 0xc0a5f3cade5c6774 => 95
	i64 13959074834287824816, ; 195: Xamarin.AndroidX.Fragment => 0xc1b8989a7ad20fb0 => 61
	i64 14100563506285742564, ; 196: da/Microsoft.Maui.Controls.resources.dll => 0xc3af43cd0cff89e4 => 3
	i64 14124974489674258913, ; 197: Xamarin.AndroidX.CardView => 0xc405fd76067d19e1 => 54
	i64 14125464355221830302, ; 198: System.Threading.dll => 0xc407bafdbc707a9e => 128
	i64 14165531176311179688, ; 199: Xamarin.Firebase.Auth => 0xc496138d7abfc9a8 => 78
	i64 14382082037123372364, ; 200: Xamarin.Firebase.Auth.Interop => 0xc7976b69c943d54c => 79
	i64 14461014870687870182, ; 201: System.Net.Requests.dll => 0xc8afd8683afdece6 => 108
	i64 14464374589798375073, ; 202: ru\Microsoft.Maui.Controls.resources => 0xc8bbc80dcb1e5ea1 => 24
	i64 14522721392235705434, ; 203: el/Microsoft.Maui.Controls.resources.dll => 0xc98b12295c2cf45a => 5
	i64 14551742072151931844, ; 204: System.Text.Encodings.Web.dll => 0xc9f22c50f1b8fbc4 => 123
	i64 14669215534098758659, ; 205: Microsoft.Extensions.DependencyInjection.dll => 0xcb9385ceb3993c03 => 37
	i64 14705122255218365489, ; 206: ko\Microsoft.Maui.Controls.resources => 0xcc1316c7b0fb5431 => 16
	i64 14744092281598614090, ; 207: zh-Hans\Microsoft.Maui.Controls.resources => 0xcc9d89d004439a4a => 32
	i64 14789919016435397935, ; 208: Xamarin.Firebase.Common.dll => 0xcd4058fc2f6d352f => 80
	i64 14852515768018889994, ; 209: Xamarin.AndroidX.CursorAdapter.dll => 0xce1ebc6625a76d0a => 58
	i64 14892012299694389861, ; 210: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xceab0e490a083a65 => 33
	i64 14904040806490515477, ; 211: ar\Microsoft.Maui.Controls.resources => 0xced5ca2604cb2815 => 0
	i64 14954917835170835695, ; 212: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xcf8a8a895a82ecef => 38
	i64 14984936317414011727, ; 213: System.Net.WebHeaderCollection => 0xcff5302fe54ff34f => 111
	i64 14987728460634540364, ; 214: System.IO.Compression.dll => 0xcfff1ba06622494c => 99
	i64 15015154896917945444, ; 215: System.Net.Security.dll => 0xd0608bd33642dc64 => 109
	i64 15024878362326791334, ; 216: System.Net.Http.Json => 0xd0831743ebf0f4a6 => 103
	i64 15076659072870671916, ; 217: System.ObjectModel.dll => 0xd13b0d8c1620662c => 115
	i64 15111608613780139878, ; 218: ms\Microsoft.Maui.Controls.resources => 0xd1b737f831192f66 => 17
	i64 15115185479366240210, ; 219: System.IO.Compression.Brotli.dll => 0xd1c3ed1c1bc467d2 => 98
	i64 15133485256822086103, ; 220: System.Linq.dll => 0xd204f0a9127dd9d7 => 101
	i64 15227001540531775957, ; 221: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd3512d3999b8e9d5 => 36
	i64 15370334346939861994, ; 222: Xamarin.AndroidX.Core.dll => 0xd54e65a72c560bea => 57
	i64 15391712275433856905, ; 223: Microsoft.Extensions.DependencyInjection.Abstractions => 0xd59a58c406411f89 => 38
	i64 15527772828719725935, ; 224: System.Console => 0xd77dbb1e38cd3d6f => 95
	i64 15536481058354060254, ; 225: de\Microsoft.Maui.Controls.resources => 0xd79cab34eec75bde => 4
	i64 15557562860424774966, ; 226: System.Net.Sockets => 0xd7e790fe7a6dc536 => 110
	i64 15582737692548360875, ; 227: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xd841015ed86f6aab => 65
	i64 15609085926864131306, ; 228: System.dll => 0xd89e9cf3334914ea => 130
	i64 15661133872274321916, ; 229: System.Xml.ReaderWriter.dll => 0xd9578647d4bfb1fc => 129
	i64 15664356999916475676, ; 230: de/Microsoft.Maui.Controls.resources.dll => 0xd962f9b2b6ecd51c => 4
	i64 15743187114543869802, ; 231: hu/Microsoft.Maui.Controls.resources.dll => 0xda7b09450ae4ef6a => 12
	i64 15783653065526199428, ; 232: el\Microsoft.Maui.Controls.resources => 0xdb0accd674b1c484 => 5
	i64 15847085070278954535, ; 233: System.Threading.Channels.dll => 0xdbec27e8f35f8e27 => 126
	i64 15930129725311349754, ; 234: Xamarin.GooglePlayServices.Tasks.dll => 0xdd1330956f12f3fa => 84
	i64 16018552496348375205, ; 235: System.Net.NetworkInformation.dll => 0xde4d54a020caa8a5 => 106
	i64 16154507427712707110, ; 236: System => 0xe03056ea4e39aa26 => 130
	i64 16219561732052121626, ; 237: System.Net.Security => 0xe1177575db7c781a => 109
	i64 16288847719894691167, ; 238: nb\Microsoft.Maui.Controls.resources => 0xe20d9cb300c12d5f => 18
	i64 16321164108206115771, ; 239: Microsoft.Extensions.Logging.Abstractions.dll => 0xe2806c487e7b0bbb => 40
	i64 16454459195343277943, ; 240: System.Net.NetworkInformation => 0xe459fb756d988f77 => 106
	i64 16649148416072044166, ; 241: Microsoft.Maui.Graphics => 0xe70da84600bb4e86 => 47
	i64 16677317093839702854, ; 242: Xamarin.AndroidX.Navigation.UI => 0xe771bb8960dd8b46 => 70
	i64 16833383113903931215, ; 243: mscorlib => 0xe99c30c1484d7f4f => 131
	i64 16890310621557459193, ; 244: System.Text.RegularExpressions.dll => 0xea66700587f088f9 => 125
	i64 16942731696432749159, ; 245: sk\Microsoft.Maui.Controls.resources => 0xeb20acb622a01a67 => 25
	i64 16998075588627545693, ; 246: Xamarin.AndroidX.Navigation.Fragment => 0xebe54bb02d623e5d => 68
	i64 17008137082415910100, ; 247: System.Collections.NonGeneric => 0xec090a90408c8cd4 => 89
	i64 17031351772568316411, ; 248: Xamarin.AndroidX.Navigation.Common.dll => 0xec5b843380a769fb => 67
	i64 17062143951396181894, ; 249: System.ComponentModel.Primitives => 0xecc8e986518c9786 => 92
	i64 17089008752050867324, ; 250: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xed285aeb25888c7c => 32
	i64 17118171214553292978, ; 251: System.Threading.Channels => 0xed8ff6060fc420b2 => 126
	i64 17338386382517543202, ; 252: System.Net.WebSockets.Client.dll => 0xf09e528d5c6da122 => 112
	i64 17342750010158924305, ; 253: hi\Microsoft.Maui.Controls.resources => 0xf0add33f97ecc211 => 10
	i64 17438153253682247751, ; 254: sk/Microsoft.Maui.Controls.resources.dll => 0xf200c3fe308d7847 => 25
	i64 17509662556995089465, ; 255: System.Net.WebSockets.dll => 0xf2fed1534ea67439 => 113
	i64 17514990004910432069, ; 256: fr\Microsoft.Maui.Controls.resources => 0xf311be9c6f341f45 => 8
	i64 17623389608345532001, ; 257: pl\Microsoft.Maui.Controls.resources => 0xf492db79dfbef661 => 20
	i64 17702523067201099846, ; 258: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xf5abfef008ae1846 => 31
	i64 17704177640604968747, ; 259: Xamarin.AndroidX.Loader => 0xf5b1dfc36cac272b => 66
	i64 17710060891934109755, ; 260: Xamarin.AndroidX.Lifecycle.ViewModel => 0xf5c6c68c9e45303b => 64
	i64 17712670374920797664, ; 261: System.Runtime.InteropServices.dll => 0xf5d00bdc38bd3de0 => 118
	i64 17777860260071588075, ; 262: System.Runtime.Numerics.dll => 0xf6b7a5b72419c0eb => 120
	i64 17986907704309214542, ; 263: Xamarin.GooglePlayServices.Basement.dll => 0xf99e554223166d4e => 83
	i64 18025913125965088385, ; 264: System.Threading => 0xfa28e87b91334681 => 128
	i64 18099568558057551825, ; 265: nl/Microsoft.Maui.Controls.resources.dll => 0xfb2e95b53ad977d1 => 19
	i64 18121036031235206392, ; 266: Xamarin.AndroidX.Navigation.Common => 0xfb7ada42d3d42cf8 => 67
	i64 18146411883821974900, ; 267: System.Formats.Asn1.dll => 0xfbd50176eb22c574 => 97
	i64 18245806341561545090, ; 268: System.Collections.Concurrent.dll => 0xfd3620327d587182 => 88
	i64 18305135509493619199, ; 269: Xamarin.AndroidX.Navigation.Runtime.dll => 0xfe08e7c2d8c199ff => 69
	i64 18324163916253801303, ; 270: it\Microsoft.Maui.Controls.resources => 0xfe4c81ff0a56ab57 => 14
	i64 18380184030268848184 ; 271: Xamarin.AndroidX.VersionedParcelable => 0xff1387fe3e7b7838 => 75
], align 16

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [272 x i32] [
	i32 42, ; 0
	i32 135, ; 1
	i32 46, ; 2
	i32 100, ; 3
	i32 72, ; 4
	i32 55, ; 5
	i32 73, ; 6
	i32 7, ; 7
	i32 127, ; 8
	i32 48, ; 9
	i32 94, ; 10
	i32 10, ; 11
	i32 60, ; 12
	i32 82, ; 13
	i32 18, ; 14
	i32 68, ; 15
	i32 79, ; 16
	i32 81, ; 17
	i32 107, ; 18
	i32 43, ; 19
	i32 134, ; 20
	i32 127, ; 21
	i32 16, ; 22
	i32 53, ; 23
	i32 65, ; 24
	i32 102, ; 25
	i32 116, ; 26
	i32 52, ; 27
	i32 6, ; 28
	i32 72, ; 29
	i32 28, ; 30
	i32 73, ; 31
	i32 44, ; 32
	i32 28, ; 33
	i32 64, ; 34
	i32 2, ; 35
	i32 20, ; 36
	i32 60, ; 37
	i32 88, ; 38
	i32 24, ; 39
	i32 63, ; 40
	i32 123, ; 41
	i32 56, ; 42
	i32 121, ; 43
	i32 51, ; 44
	i32 87, ; 45
	i32 27, ; 46
	i32 131, ; 47
	i32 105, ; 48
	i32 37, ; 49
	i32 2, ; 50
	i32 113, ; 51
	i32 7, ; 52
	i32 82, ; 53
	i32 62, ; 54
	i32 114, ; 55
	i32 120, ; 56
	i32 110, ; 57
	i32 86, ; 58
	i32 80, ; 59
	i32 83, ; 60
	i32 46, ; 61
	i32 35, ; 62
	i32 74, ; 63
	i32 132, ; 64
	i32 22, ; 65
	i32 121, ; 66
	i32 36, ; 67
	i32 129, ; 68
	i32 35, ; 69
	i32 71, ; 70
	i32 39, ; 71
	i32 44, ; 72
	i32 108, ; 73
	i32 102, ; 74
	i32 117, ; 75
	i32 33, ; 76
	i32 94, ; 77
	i32 100, ; 78
	i32 93, ; 79
	i32 84, ; 80
	i32 30, ; 81
	i32 75, ; 82
	i32 0, ; 83
	i32 81, ; 84
	i32 51, ; 85
	i32 74, ; 86
	i32 105, ; 87
	i32 119, ; 88
	i32 90, ; 89
	i32 90, ; 90
	i32 119, ; 91
	i32 50, ; 92
	i32 26, ; 93
	i32 29, ; 94
	i32 98, ; 95
	i32 122, ; 96
	i32 77, ; 97
	i32 97, ; 98
	i32 23, ; 99
	i32 23, ; 100
	i32 124, ; 101
	i32 34, ; 102
	i32 63, ; 103
	i32 78, ; 104
	i32 11, ; 105
	i32 59, ; 106
	i32 41, ; 107
	i32 19, ; 108
	i32 22, ; 109
	i32 114, ; 110
	i32 26, ; 111
	i32 101, ; 112
	i32 93, ; 113
	i32 115, ; 114
	i32 87, ; 115
	i32 49, ; 116
	i32 104, ; 117
	i32 17, ; 118
	i32 132, ; 119
	i32 85, ; 120
	i32 53, ; 121
	i32 91, ; 122
	i32 62, ; 123
	i32 103, ; 124
	i32 96, ; 125
	i32 133, ; 126
	i32 91, ; 127
	i32 47, ; 128
	i32 122, ; 129
	i32 70, ; 130
	i32 116, ; 131
	i32 21, ; 132
	i32 133, ; 133
	i32 69, ; 134
	i32 21, ; 135
	i32 96, ; 136
	i32 31, ; 137
	i32 50, ; 138
	i32 77, ; 139
	i32 52, ; 140
	i32 125, ; 141
	i32 56, ; 142
	i32 89, ; 143
	i32 135, ; 144
	i32 45, ; 145
	i32 6, ; 146
	i32 104, ; 147
	i32 117, ; 148
	i32 112, ; 149
	i32 43, ; 150
	i32 3, ; 151
	i32 59, ; 152
	i32 86, ; 153
	i32 57, ; 154
	i32 1, ; 155
	i32 107, ; 156
	i32 49, ; 157
	i32 40, ; 158
	i32 45, ; 159
	i32 12, ; 160
	i32 76, ; 161
	i32 15, ; 162
	i32 41, ; 163
	i32 55, ; 164
	i32 58, ; 165
	i32 118, ; 166
	i32 13, ; 167
	i32 76, ; 168
	i32 39, ; 169
	i32 9, ; 170
	i32 48, ; 171
	i32 124, ; 172
	i32 61, ; 173
	i32 66, ; 174
	i32 34, ; 175
	i32 54, ; 176
	i32 99, ; 177
	i32 14, ; 178
	i32 27, ; 179
	i32 42, ; 180
	i32 111, ; 181
	i32 1, ; 182
	i32 15, ; 183
	i32 134, ; 184
	i32 9, ; 185
	i32 85, ; 186
	i32 29, ; 187
	i32 30, ; 188
	i32 13, ; 189
	i32 71, ; 190
	i32 92, ; 191
	i32 8, ; 192
	i32 11, ; 193
	i32 95, ; 194
	i32 61, ; 195
	i32 3, ; 196
	i32 54, ; 197
	i32 128, ; 198
	i32 78, ; 199
	i32 79, ; 200
	i32 108, ; 201
	i32 24, ; 202
	i32 5, ; 203
	i32 123, ; 204
	i32 37, ; 205
	i32 16, ; 206
	i32 32, ; 207
	i32 80, ; 208
	i32 58, ; 209
	i32 33, ; 210
	i32 0, ; 211
	i32 38, ; 212
	i32 111, ; 213
	i32 99, ; 214
	i32 109, ; 215
	i32 103, ; 216
	i32 115, ; 217
	i32 17, ; 218
	i32 98, ; 219
	i32 101, ; 220
	i32 36, ; 221
	i32 57, ; 222
	i32 38, ; 223
	i32 95, ; 224
	i32 4, ; 225
	i32 110, ; 226
	i32 65, ; 227
	i32 130, ; 228
	i32 129, ; 229
	i32 4, ; 230
	i32 12, ; 231
	i32 5, ; 232
	i32 126, ; 233
	i32 84, ; 234
	i32 106, ; 235
	i32 130, ; 236
	i32 109, ; 237
	i32 18, ; 238
	i32 40, ; 239
	i32 106, ; 240
	i32 47, ; 241
	i32 70, ; 242
	i32 131, ; 243
	i32 125, ; 244
	i32 25, ; 245
	i32 68, ; 246
	i32 89, ; 247
	i32 67, ; 248
	i32 92, ; 249
	i32 32, ; 250
	i32 126, ; 251
	i32 112, ; 252
	i32 10, ; 253
	i32 25, ; 254
	i32 113, ; 255
	i32 8, ; 256
	i32 20, ; 257
	i32 31, ; 258
	i32 66, ; 259
	i32 64, ; 260
	i32 118, ; 261
	i32 120, ; 262
	i32 83, ; 263
	i32 128, ; 264
	i32 19, ; 265
	i32 67, ; 266
	i32 97, ; 267
	i32 88, ; 268
	i32 69, ; 269
	i32 14, ; 270
	i32 75 ; 271
], align 16

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

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
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 16

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" }

; Metadata
!llvm.module.flags = !{!0, !1}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
