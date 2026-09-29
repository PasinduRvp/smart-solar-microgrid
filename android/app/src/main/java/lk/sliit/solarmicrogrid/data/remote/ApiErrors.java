/*
 * ---------------------------------------------------------------------------
 * File        : ApiErrors.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : Turns a failed API response into one line the user can read.
 *
 * Why it matters
 *               The Web API already writes good messages. For example
 *               Bookings must be at least 12 hours away. Or
 *               That booking window is full.
 *               Those sentences are the business rules speaking.
 *               Showing Error 400 instead would waste them. The user would
 *               not know what to do next.
 *
 * SOLID        Single Responsibility. It reads an error body and returns
 *               text. It shows nothing and logs nothing.
 *
 * Order used   1. the first validation message, if a field was rejected
 *               2. detail, where business rule messages go
 *               3. title, the general name of the status code
 *               4. a fallback that names the status code
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.google.gson.Gson;

import java.util.List;
import java.util.Map;

import lk.sliit.solarmicrogrid.data.remote.dto.ProblemDetails;
import okhttp3.ResponseBody;
import retrofit2.Response;

public final class ApiErrors {

    private static final Gson GSON = new Gson();

    /**
     * Reads the message out of a failed response.
     *
     * @param response a Retrofit response where isSuccessful() is false.
     * @return one sentence, ready for a Toast or an error label.
     */
    @NonNull
    public static String messageFrom(@Nullable Response<?> response) {

        if (response == null) {
            return "Could not reach the server. Check the connection and try again.";
        }

        ProblemDetails problem = parse(response.errorBody());

        if (problem != null) {

            String validationMessage = firstValidationMessage(problem);
            if (isPresent(validationMessage)) {
                return validationMessage;
            }
            if (isPresent(problem.getDetail())) {
                return problem.getDetail();
            }
            if (isPresent(problem.getTitle())) {
                return problem.getTitle();
            }
        }

        return describeStatus(response.code());
    }

    /**
     * The body may be empty, or may not be problem details at all.
     * That must not crash the app. A failed parse just means there was
     * nothing useful inside.
     */
    @Nullable
    private static ProblemDetails parse(@Nullable ResponseBody errorBody) {
        if (errorBody == null) {
            return null;
        }
        try {
            return GSON.fromJson(errorBody.charStream(), ProblemDetails.class);
        } catch (RuntimeException parseFailed) {
            return null;
        }
    }

    @Nullable
    private static String firstValidationMessage(@NonNull ProblemDetails problem) {

        Map<String, List<String>> errors = problem.getErrors();
        if (errors == null || errors.isEmpty()) {
            return null;
        }

        for (List<String> messagesForOneField : errors.values()) {
            if (messagesForOneField != null && !messagesForOneField.isEmpty()) {
                return messagesForOneField.get(0);
            }
        }
        return null;
    }

    /**
     * The last resort wording.
     * It tells the user what to do, not what broke inside.
     */
    @NonNull
    private static String describeStatus(int statusCode) {
        switch (statusCode) {
            case 401:
                return "Your details were not recognised. Check them and try again.";
            case 403:
                return "This account is not allowed to do that.";
            case 404:
                return "That item no longer exists.";
            case 409:
                return "That change conflicts with something already saved.";
            case 429:
                return "Too many attempts. Wait a moment and try again.";
            default:
                return "The server could not complete that request (" + statusCode + ").";
        }
    }

    private static boolean isPresent(@Nullable String value) {
        return value != null && !value.trim().isEmpty();
    }

    /** Utility class: never instantiated. */
    private ApiErrors() {
    }
}
