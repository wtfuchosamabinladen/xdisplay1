package com.xdisplay;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.text.TextUtils;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

public class MainActivity extends Activity {

    private EditText ipInput;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        ipInput = (EditText) findViewById(R.id.ip_input);
        Button wifiBtn = (Button) findViewById(R.id.wifi_btn);
        Button usbBtn  = (Button) findViewById(R.id.usb_btn);

        wifiBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String ip = ipInput.getText().toString().trim();
                if (TextUtils.isEmpty(ip)) {
                    Toast.makeText(MainActivity.this,
                        "Please enter your Windows PC IP address", Toast.LENGTH_SHORT).show();
                    return;
                }
                launchStream(ip);
            }
        });

        usbBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                // USB uses ADB port forwarding — connect to localhost
                Toast.makeText(MainActivity.this,
                    "Connecting via USB (localhost)...", Toast.LENGTH_SHORT).show();
                launchStream("127.0.0.1");
            }
        });
    }

    private void launchStream(String ip) {
        Intent intent = new Intent(this, StreamActivity.class);
        intent.putExtra(StreamActivity.EXTRA_IP, ip);
        startActivity(intent);
    }
}
