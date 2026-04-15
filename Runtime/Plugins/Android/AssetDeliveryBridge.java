package com.bizsim.google.play.assetdelivery;

import android.app.Activity;
import android.os.Handler;
import android.os.Looper;

import com.google.android.play.core.assetpacks.AssetPackLocation;
import com.google.android.play.core.assetpacks.AssetPackManager;
import com.google.android.play.core.assetpacks.AssetPackManagerFactory;
import com.google.android.play.core.assetpacks.AssetPackState;
import com.google.android.play.core.assetpacks.AssetPackStates;

import com.google.android.gms.tasks.Task;

import java.util.Arrays;
import java.util.Map;

public final class AssetDeliveryBridge {
    private static final String TAG = "BizSimAssetDelivery";

    // Callback interfaces — implemented in C# via AndroidJavaProxy.

    public interface IFetchCallback {
        void onFetchSettled(String[] packNames, int[] statuses, int[] errorCodes,
                            long[] bytesDownloaded, long[] totalBytesToDownload);
        void onFetchError(int errorCode, String message);
    }

    public interface IGetStatesCallback {
        void onStatesReceived(String[] packNames, int[] statuses, int[] errorCodes,
                              long[] bytesDownloaded, long[] totalBytesToDownload);
        void onStatesError(int errorCode, String message);
    }

    public interface IPackLocationCallback {
        // Returned synchronously on the caller's thread; see getPackLocation below.
        void onLocationReceived(String packName, String path, String assetsPath, int storageMethod);
        void onNotInstalled(String packName);
    }

    public interface IRemovePackCallback {
        void onRemoveInvoked(String packName);
        void onRemoveError(int errorCode, String message);
    }

    public interface IConfirmationDialogCallback {
        void onDialogResult(int resultCode);    // Activity.RESULT_OK / RESULT_CANCELED
        void onDialogError(int errorCode, String message);
    }

    public interface IStateUpdateCallback {
        void onStateUpdate(String packName, int status, int errorCode,
                           long bytesDownloaded, long totalBytesToDownload, int transferProgressPercentage);
    }

    private static AssetDeliveryBridge sInstance;
    public static synchronized AssetDeliveryBridge getInstance() { return sInstance; }

    private final AssetPackManager manager;
    private final Handler mainHandler;
    private final AssetPackStateListenerBridge listenerBridge;

    public static synchronized AssetDeliveryBridge init(Activity activity) {
        if (sInstance == null) {
            sInstance = new AssetDeliveryBridge(activity);
        }
        return sInstance;
    }

    private AssetDeliveryBridge(Activity activity) {
        this.mainHandler = new Handler(Looper.getMainLooper());
        this.manager = AssetPackManagerFactory.getInstance(activity.getApplicationContext());
        this.listenerBridge = new AssetPackStateListenerBridge(manager);
    }

    public void setStateUpdateCallback(IStateUpdateCallback cb) {
        listenerBridge.setCallback(cb);
        listenerBridge.register();
    }

    public void unregisterStateListener() {
        listenerBridge.unregister();
    }

    public void fetch(String[] packNames, IFetchCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<AssetPackStates> task = manager.fetch(Arrays.asList(packNames));
                task.addOnSuccessListener(states -> cb.onFetchSettled(
                    extractNames(states), extractStatuses(states), extractErrorCodes(states),
                    extractBytesDownloaded(states), extractTotalBytes(states)));
                task.addOnFailureListener(e ->
                    cb.onFetchError(-100, e.getMessage() == null ? "" : e.getMessage()));
            } catch (Throwable thr) {
                cb.onFetchError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    public void getPackStates(String[] packNames, IGetStatesCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<AssetPackStates> task = manager.getPackStates(Arrays.asList(packNames));
                task.addOnSuccessListener(states -> cb.onStatesReceived(
                    extractNames(states), extractStatuses(states), extractErrorCodes(states),
                    extractBytesDownloaded(states), extractTotalBytes(states)));
                task.addOnFailureListener(e ->
                    cb.onStatesError(-100, e.getMessage() == null ? "" : e.getMessage()));
            } catch (Throwable thr) {
                cb.onStatesError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    // Synchronous — getPackLocation is not a Task-returning API.
    public void getPackLocation(String packName, IPackLocationCallback cb) {
        try {
            AssetPackLocation loc = manager.getPackLocation(packName);
            if (loc == null) { cb.onNotInstalled(packName); return; }
            cb.onLocationReceived(packName, loc.path(), loc.assetsPath(), loc.packStorageMethod());
        } catch (Throwable thr) {
            cb.onNotInstalled(packName);
        }
    }

    public void cancel(String[] packNames) {
        mainHandler.post(() -> {
            try { manager.cancel(Arrays.asList(packNames)); }
            catch (Throwable thr) { android.util.Log.w(TAG, "cancel() threw: " + thr.getMessage()); }
        });
    }

    public void removePack(String packName, IRemovePackCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<Void> task = manager.removePack(packName);
                task.addOnSuccessListener(unused -> cb.onRemoveInvoked(packName));
                task.addOnFailureListener(e ->
                    cb.onRemoveError(-100, e.getMessage() == null ? "" : e.getMessage()));
            } catch (Throwable thr) {
                cb.onRemoveError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    public void showConfirmationDialog(Activity activity, IConfirmationDialogCallback cb) {
        mainHandler.post(() -> {
            try {
                Task<Integer> task = manager.showConfirmationDialog(activity);
                task.addOnSuccessListener(result -> cb.onDialogResult(result));
                task.addOnFailureListener(e ->
                    cb.onDialogError(-100, e.getMessage() == null ? "" : e.getMessage()));
            } catch (Throwable thr) {
                cb.onDialogError(-100, thr.getMessage() == null ? "" : thr.getMessage());
            }
        });
    }

    // ---- private helpers: flatten AssetPackStates into parallel primitive arrays ----

    private static String[] extractNames(AssetPackStates states) {
        Map<String, AssetPackState> map = states.packStates();
        return map.keySet().toArray(new String[0]);
    }

    private static int[] extractStatuses(AssetPackStates states) {
        Map<String, AssetPackState> map = states.packStates();
        int[] out = new int[map.size()];
        int i = 0;
        for (AssetPackState s : map.values()) out[i++] = s.status();
        return out;
    }

    private static int[] extractErrorCodes(AssetPackStates states) {
        Map<String, AssetPackState> map = states.packStates();
        int[] out = new int[map.size()];
        int i = 0;
        for (AssetPackState s : map.values()) out[i++] = s.errorCode();
        return out;
    }

    private static long[] extractBytesDownloaded(AssetPackStates states) {
        Map<String, AssetPackState> map = states.packStates();
        long[] out = new long[map.size()];
        int i = 0;
        for (AssetPackState s : map.values()) out[i++] = s.bytesDownloaded();
        return out;
    }

    private static long[] extractTotalBytes(AssetPackStates states) {
        Map<String, AssetPackState> map = states.packStates();
        long[] out = new long[map.size()];
        int i = 0;
        for (AssetPackState s : map.values()) out[i++] = s.totalBytesToDownload();
        return out;
    }
}
