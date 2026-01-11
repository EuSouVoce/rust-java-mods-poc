package com.rustjavamods;

import com.google.gson.JsonObject;

/**
 * Game event from Rust (Facepunch game)
 */
public class GameEvent {
    private final String name;
    private final JsonObject data;

    public GameEvent(String name, JsonObject data) {
        this.name = name;
        this.data = data;
    }

    public String getName() {
        return name;
    }

    public JsonObject getData() {
        return data;
    }

    @Override
    public String toString() {
        return "GameEvent{name='" + name + "', data=" + data + '}';
    }
}
