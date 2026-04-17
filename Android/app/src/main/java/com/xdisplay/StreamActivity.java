package com.xdisplay;

import android.app.Activity;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.RectF;
import android.os.Bundle;
import android.util.Log;
import android.view.MotionEvent;
import android.view.SurfaceHolder;
import android.view.SurfaceView;
import android.view.View;
import android.view.WindowManager;
import android.widget.Toast;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.Socket;

public class StreamActivity extends Activity implements SurfaceHolder.Callback {

    public static final String EXTRA_IP = "ip";
    private static final String TAG = "XDisplay";
    private static final int VIDEO_PORT = 5555;
    private static final int INPUT_PORT = 5556;
    // Magic header value sent by Windows server
    private static final int HEADER_MAGIC = -1;

    private SurfaceView surfaceView;
    private SurfaceHolder surfaceHolder;
    private String serverIp;

    private Socket videoSocket;
    private Socket inputSocket;
    private DataInputStream videoIn;
    private DataOutputStream inputOut;

    private volatile boolean running = false;
    private int remoteWidth  = 1920;
    private int remoteHeight = 1080;
    private Paint paint;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Keep screen on and go full-screen
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);

        serverIp = getIntent().getStringExtra(EXTRA_IP);
        paint = new Paint(Paint.FILTER_BITMAP_FLAG);

        surfaceView = new SurfaceView(this);
        setContentView(surfaceView);
        surfaceHolder = surfaceView.getHolder();
        surfaceHolder.addCallback(this);

        surfaceView.setOnTouchListener(new View.OnTouchListener() {
            @Override
            public boolean onTouch(View v, MotionEvent event) {
                handleTouch(event);
                return true;
            }
        });
    }

    // ─── SurfaceHolder.Callback ───────────────────────────────────────────────

    @Override
    public void surfaceCreated(SurfaceHolder holder) {
        running = true;
        new Thread(new Runnable() {
            @Override
            public void run() {
                connectAndStream();
            }
        }).start();
    }

    @Override
    public void surfaceChanged(SurfaceHolder holder, int format, int width, int height) {
        // Nothing to do
    }

    @Override
    public void surfaceDestroyed(SurfaceHolder holder) {
        running = false;
        closeConnections();
    }

    // ─── Networking ───────────────────────────────────────────────────────────

    private void connectAndStream() {
        try {
            // Open video (receive) socket
            videoSocket = new Socket(serverIp, VIDEO_PORT);
            videoSocket.setTcpNoDelay(true);
            videoSocket.setReceiveBufferSize(2 * 1024 * 1024);
            videoIn = new DataInputStream(videoSocket.getInputStream());

            // Open input (send) socket
            inputSocket = new Socket(serverIp, INPUT_PORT);
            inputSocket.setTcpNoDelay(true);
            inputOut = new DataOutputStream(inputSocket.getOutputStream());

            // Read resolution header sent by Windows server
            int magic = videoIn.readInt();
            if (magic == HEADER_MAGIC) {
                remoteWidth  = videoIn.readInt();
                remoteHeight = videoIn.readInt();
            }
            // If magic doesn't match, server may be old version — use defaults

            showToast("Connected!  " + remoteWidth + "×" + remoteHeight);

            streamLoop();

        } catch (final Exception e) {
            Log.e(TAG, "Connection error", e);
            showToast("Could not connect: " + e.getMessage());
            finish();
        }
    }

    private void streamLoop() throws IOException {
        while (running) {
            // Read frame length (4 bytes, big-endian — matches C# WriteIntBE)
            int frameLen = videoIn.readInt();

            // Sanity check — skip bad frames
            if (frameLen <= 0 || frameLen > 12 * 1024 * 1024) {
                Log.w(TAG, "Bad frame length: " + frameLen);
                continue;
            }

            // Read exactly frameLen bytes
            byte[] data = new byte[frameLen];
            int read = 0;
            while (read < frameLen) {
                int n = videoIn.read(data, read, frameLen - read);
                if (n < 0) return; // stream closed
                read += n;
            }

            // Decode JPEG
            Bitmap bmp = BitmapFactory.decodeByteArray(data, 0, data.length);
            if (bmp == null) continue;

            // Draw onto surface, scaled to fit
            Canvas canvas = surfaceHolder.lockCanvas();
            if (canvas != null) {
                try {
                    RectF dst = new RectF(0, 0, canvas.getWidth(), canvas.getHeight());
                    canvas.drawBitmap(bmp, null, dst, paint);
                } finally {
                    surfaceHolder.unlockCanvasAndPost(canvas);
                }
            }
            bmp.recycle();
        }
    }

    // ─── Touch → Mouse ────────────────────────────────────────────────────────

    private void handleTouch(MotionEvent event) {
        if (inputOut == null) return;

        // Map touch position to remote screen coordinates
        final int x = (int)(event.getX() / surfaceView.getWidth()  * remoteWidth);
        final int y = (int)(event.getY() / surfaceView.getHeight() * remoteHeight);

        final byte type;
        int action = event.getActionMasked();
        if      (action == MotionEvent.ACTION_DOWN) type = 1; // mouse down
        else if (action == MotionEvent.ACTION_UP)   type = 2; // mouse up
        else                                         type = 0; // move

        // Send on a background thread so we don't block UI
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    inputOut.writeByte(type);
                    inputOut.writeInt(x);   // big-endian, matches C# ReadIntBE
                    inputOut.writeInt(y);
                    inputOut.flush();
                } catch (IOException e) {
                    Log.e(TAG, "Input send failed", e);
                }
            }
        }).start();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void closeConnections() {
        try { if (videoSocket != null) videoSocket.close(); } catch (Exception ignored) {}
        try { if (inputSocket != null) inputSocket.close(); } catch (Exception ignored) {}
    }

    private void showToast(final String msg) {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Toast.makeText(StreamActivity.this, msg, Toast.LENGTH_SHORT).show();
            }
        });
    }

    @Override
    public void onBackPressed() {
        running = false;
        closeConnections();
        super.onBackPressed();
    }
}
