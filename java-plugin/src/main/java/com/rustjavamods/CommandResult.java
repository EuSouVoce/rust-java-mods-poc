package com.rustjavamods;

/**
 * Result of a Java->Carbon command execution.
 */
public record CommandResult(boolean success, String errorMessage) {
    public static CommandResult ok() {
        return new CommandResult(true, null);
    }

    public static CommandResult error(String message) {
        return new CommandResult(false, message);
    }
}
