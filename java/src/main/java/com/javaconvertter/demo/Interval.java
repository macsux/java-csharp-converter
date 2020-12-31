package com.javaconvertter.demo;

import lombok.*;

@Value
@AllArgsConstructor
public class Interval {
    private int start;
    // non-inclusive of character
    private int stop;
}
