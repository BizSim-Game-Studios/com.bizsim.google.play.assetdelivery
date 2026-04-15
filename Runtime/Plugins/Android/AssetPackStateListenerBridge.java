package com.bizsim.google.play.assetdelivery;

import com.google.android.play.core.assetpacks.AssetPackManager;
import com.google.android.play.core.assetpacks.AssetPackState;
import com.google.android.play.core.assetpacks.AssetPackStateUpdateListener;

public final class AssetPackStateListenerBridge {
    private final AssetPackManager manager;
    private AssetDeliveryBridge.IStateUpdateCallback callback;
    private AssetPackStateUpdateListener listener;
    private boolean registered;

    public AssetPackStateListenerBridge(AssetPackManager manager) {
        this.manager = manager;
    }

    public void setCallback(AssetDeliveryBridge.IStateUpdateCallback cb) {
        this.callback = cb;
    }

    public synchronized void register() {
        if (registered) return;
        listener = new AssetPackStateUpdateListener() {
            @Override
            public void onStateUpdate(AssetPackState state) {
                if (callback == null) return;
                callback.onStateUpdate(
                    state.name(),
                    state.status(),
                    state.errorCode(),
                    state.bytesDownloaded(),
                    state.totalBytesToDownload(),
                    state.transferProgressPercentage());
            }
        };
        manager.registerListener(listener);
        registered = true;
    }

    public synchronized void unregister() {
        if (!registered || listener == null) return;
        manager.unregisterListener(listener);
        listener = null;
        registered = false;
    }
}
