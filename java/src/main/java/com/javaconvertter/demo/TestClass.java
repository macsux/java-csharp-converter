package com.javaconvertter.demo;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public abstract class TestClass<T, A extends ArrayList<T> & List<T> & Set<T>> extends ArrayList<HashSet<? super T>> {



}