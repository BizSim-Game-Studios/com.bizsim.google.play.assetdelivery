-keep class com.google.android.play.core.assetpacks.** { *; }
-keep class com.google.android.play.core.assetpacks.model.** { *; }
-keep class com.google.android.gms.tasks.** { *; }

# BizSim bridge classes — called from Unity C# via AndroidJavaProxy
-keep class com.bizsim.google.play.assetdelivery.AssetDeliveryBridge { *; }
-keep class com.bizsim.google.play.assetdelivery.AssetDeliveryBridge$* { *; }
-keep class com.bizsim.google.play.assetdelivery.AssetPackStateListenerBridge { *; }
