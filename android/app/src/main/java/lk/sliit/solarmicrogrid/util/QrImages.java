/*
 * ---------------------------------------------------------------------------
 * File        : QrImages.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : L K P Yasith (IT 23380196)
 * Created     : 2026-09-20
 * Description : Draws a QR code as a picture. Task E3.
 *
 * What it does and does not do
 *              It turns text into black and white squares. That is all.
 *              It does not make the token, and it does not check one. The
 *              server makes the token when a booking is approved.
 *
 * Always black on white
 *              The theme colours are not used here on purpose. A scanner
 *              needs strong contrast, and an amber code on a dark grey card
 *              can fail to read. Black on white works on every scanner,
 *              including in dark mode.
 *
 * SOLID       Single Responsibility. It draws one picture.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.util;

import android.graphics.Bitmap;
import android.graphics.Color;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.google.zxing.BarcodeFormat;
import com.google.zxing.EncodeHintType;
import com.google.zxing.WriterException;
import com.google.zxing.common.BitMatrix;
import com.google.zxing.qrcode.QRCodeWriter;
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel;

import java.util.EnumMap;
import java.util.Map;

public final class QrImages {

    /** The white border a scanner needs around the squares, in modules. */
    private static final int QUIET_ZONE = 2;

    /**
     * Draws the text as a QR code.
     *
     * @param text     what goes inside the code.
     * @param sizePx   the width and height of the picture, in pixels.
     * @return the picture, or null when the text could not be drawn.
     *         A null is handled by the screen. It never crashes.
     */
    @Nullable
    public static Bitmap draw(@NonNull String text, int sizePx) {

        if (text.trim().isEmpty() || sizePx <= 0) {
            return null;
        }

        Map<EncodeHintType, Object> hints = new EnumMap<>(EncodeHintType.class);

        // Level M survives a scratched screen or a poor camera, and still
        // keeps the squares large enough to read at this size.
        hints.put(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M);
        hints.put(EncodeHintType.MARGIN, QUIET_ZONE);
        hints.put(EncodeHintType.CHARACTER_SET, "UTF-8");

        try {
            BitMatrix matrix = new QRCodeWriter()
                    .encode(text, BarcodeFormat.QR_CODE, sizePx, sizePx, hints);

            return toBitmap(matrix);

        } catch (WriterException | IllegalArgumentException cannotDraw) {
            // Thrown when the text is too long for one code, or the size is
            // too small for the number of squares. Neither should crash the
            // screen, so the caller is told there is no picture.
            return null;
        }
    }

    /**
     * Copies the black and white grid into a picture.
     *
     * The pixels are written one row at a time with setPixels, not one at a
     * time with setPixel. A 600 by 600 code is 360,000 pixels, and setting
     * them one by one is slow enough to be seen as a pause.
     */
    @NonNull
    private static Bitmap toBitmap(@NonNull BitMatrix matrix) {

        int width = matrix.getWidth();
        int height = matrix.getHeight();
        int[] pixels = new int[width * height];

        for (int y = 0; y < height; y++) {
            int rowStart = y * width;
            for (int x = 0; x < width; x++) {
                pixels[rowStart + x] = matrix.get(x, y) ? Color.BLACK : Color.WHITE;
            }
        }

        Bitmap bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.RGB_565);
        bitmap.setPixels(pixels, 0, width, 0, 0, width, height);
        return bitmap;
    }

    /** Utility class: never instantiated. */
    private QrImages() {
    }
}
