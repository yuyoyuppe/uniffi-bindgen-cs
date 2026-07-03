/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

//! Exercises the `high_performance_strings` span fast path: string/bytes arguments cross as
//! pinned `ReadOnlySpan<byte>` pairs through the `_raw` scaffolding exports emitted by the
//! companion uniffi-rs fork. This crate opts in via its `uniffi.toml`; every other fixture
//! stays on the standard RustBuffer path, so both codegen modes are covered by the suite.

use std::sync::{Arc, Mutex};

uniffi::setup_scaffolding!();

#[uniffi::export]
fn span_echo_string(value: String) -> String {
    value
}

#[uniffi::export]
fn span_echo_bytes(value: Vec<u8>) -> Vec<u8> {
    value
}

/// Mixed span and non-span arguments, in an order that interleaves them.
#[uniffi::export]
fn span_describe(prefix: String, count: u32, payload: Vec<u8>, upper: bool) -> String {
    let text = format!("{prefix}:{count}:{}", payload.len());
    if upper {
        text.to_uppercase()
    } else {
        text
    }
}

#[derive(Debug, uniffi::Error)]
pub enum SpanError {
    Boom { message: String },
}

impl std::fmt::Display for SpanError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            SpanError::Boom { message } => write!(f, "{message}"),
        }
    }
}

impl std::error::Error for SpanError {}

/// Always fails: proves errors propagate through the `_raw` call-status path.
#[uniffi::export]
fn span_fail(message: String) -> Result<String, SpanError> {
    Err(SpanError::Boom { message })
}

#[derive(uniffi::Object)]
pub struct SpanRecorder {
    value: Mutex<String>,
}

#[uniffi::export]
impl SpanRecorder {
    #[uniffi::constructor]
    pub fn new() -> Arc<Self> {
        Arc::new(Self {
            value: Mutex::new(String::new()),
        })
    }

    pub fn append(&self, chunk: String) {
        self.value.lock().unwrap().push_str(&chunk);
    }

    pub fn append_bytes(&self, chunk: Vec<u8>) {
        self.value
            .lock()
            .unwrap()
            .push_str(&format!("[{} bytes]", chunk.len()));
    }

    /// No span-eligible arguments: stays on the standard path even with the flag on.
    pub fn value(&self) -> String {
        self.value.lock().unwrap().clone()
    }

    pub fn fail(&self, message: String) -> Result<(), SpanError> {
        Err(SpanError::Boom { message })
    }
}
