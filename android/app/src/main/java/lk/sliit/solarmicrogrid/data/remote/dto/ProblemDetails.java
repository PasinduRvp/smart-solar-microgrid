/*
 * ---------------------------------------------------------------------------
 * File        : ProblemDetails.java
 * Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
 * Author      : <Your Full Name> (<IT Number>)
 * Created     : 2026-09-20
 * Description : The shape of an error body from the Web API.
 *
 * Why it exists
 *               ASP.NET Core reports failures as RFC 7807 problem details.
 *               A business rule refusal arrives like this:
 *               {"title": "...", "detail": "That booking window is full"}
 *               A validation failure also has an errors map. That map holds
 *               a field name and what is wrong with it.
 *               Reading this is what lets the app show the server's own
 *               words instead of a useless Error 400.
 *
 * See          ApiErrors. It turns one of these into a single line of text.
 * ---------------------------------------------------------------------------
 */
package lk.sliit.solarmicrogrid.data.remote.dto;

import java.util.List;
import java.util.Map;

public final class ProblemDetails {

    private String type;
    private String title;
    private Integer status;
    private String detail;

    /** A field name, and what is wrong with it. Null unless a field was rejected. */
    private Map<String, List<String>> errors;

    public String getType() {
        return type;
    }

    public String getTitle() {
        return title;
    }

    public Integer getStatus() {
        return status;
    }

    public String getDetail() {
        return detail;
    }

    public Map<String, List<String>> getErrors() {
        return errors;
    }
}
