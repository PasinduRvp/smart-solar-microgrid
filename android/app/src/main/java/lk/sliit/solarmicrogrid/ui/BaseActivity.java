/*
 * ---------------------------------------------------------------------------
 * File        : BaseActivity.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : What every screen does before it draws.
 *
 * The problem this fixes
 *               The app targets SDK 35. From Android 15 the system no
 *               longer leaves a gap for the status bar and the navigation
 *               bar. Every app draws edge to edge, asked for or not.
 *               A layout that ignores this has its first line of text under
 *               the clock. Its last button sits under the gesture bar.
 *
 * The fix      Ask the system how tall those bars are. Pad the root view by
 *               that much. The background still reaches the screen edge,
 *               which is the point of edge to edge. No content is hidden.
 *
 * Why turn it on everywhere
 *               EdgeToEdge.enable() gives the same behaviour on older
 *               versions too. One behaviour on every Android version is much
 *               easier to think about than padding that only happens on a
 *               new phone.
 *
 * Why not the keyboard
 *               The keyboard is handled by android:windowSoftInputMode
 *               ="adjustResize" in the manifest. That shrinks the window.
 *               Padding for it here as well would move the content twice.
 *
 * OOP          Every screen inherits this. So a new screen is correct even
 *               if its author does not know any of the above.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.ui;

import android.os.Bundle;
import android.view.View;

import androidx.activity.EdgeToEdge;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.graphics.Insets;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowInsetsCompat;

public abstract class BaseActivity extends AppCompatActivity {

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        // Before super.onCreate, which is when the window is configured.
        EdgeToEdge.enable(this);
        super.onCreate(savedInstanceState);
    }

    /**
     * Keeps the content clear of the status bar and the navigation bar.
     *
     * Call this once, from onCreate. Pass the root view of the layout.
     *
     * @param root the outermost view of the layout.
     */
    protected void applySystemBarInsets(@NonNull View root) {

        // Read once, before any bar height is added.
        // The listener runs again on rotation, and whenever the bars change.
        // Reading the padding inside it would add the bar height to a value
        // that already had it. The gap would grow a little each time.
        final int paddingLeft = root.getPaddingLeft();
        final int paddingTop = root.getPaddingTop();
        final int paddingRight = root.getPaddingRight();
        final int paddingBottom = root.getPaddingBottom();

        ViewCompat.setOnApplyWindowInsetsListener(root, (view, windowInsets) -> {

            Insets bars = windowInsets.getInsets(WindowInsetsCompat.Type.systemBars());

            // The padding of the layout is kept. The bar heights are added
            // to it. A screen with a 24dp gutter does not lose it.
            view.setPadding(
                    paddingLeft + bars.left,
                    paddingTop + bars.top,
                    paddingRight + bars.right,
                    paddingBottom + bars.bottom);

            // CONSUMED stops a child view using the same bar heights
            // again. That would double the gap.
            return WindowInsetsCompat.CONSUMED;
        });
    }
}
