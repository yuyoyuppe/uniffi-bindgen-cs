{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- call cs::docstring(func, 4) %}
{%- call cs::method_throws_annotation(func.throws_type()) %}
{%- if func.is_async() %}
   public static async {% call cs::return_type(func) %} {{ func.name()|fn_name }}({%- call cs::arg_list_decl(func) -%})
   {
        {%- call cs::async_call(func, false) %}
   }
{%- else %}
{%- match func.return_type() -%}
{%- when Some with (return_type) %}
    public static {{ return_type|type_name(ci) }} {{ func.name()|fn_name }}({%- call cs::arg_list_decl(func) -%}) {
        {%- if config.high_performance_strings() && func|has_string_arguments %}
        {#/* Delegate to the span variant, encoding string arguments as UTF-8 */#}
        {%- for arg in func.arguments() %}
        {%- if arg|type_name(ci) == "string" %}
        var {{ arg.name()|var_name }}Utf8 = System.Text.Encoding.UTF8.GetBytes({{ arg.name()|var_name }});
        {%- endif %}
        {%- endfor %}
        return {{ func.name()|fn_name }}Span(
            {%- for arg in func.arguments() %}
            {%- if arg|type_name(ci) == "string" %}
            {{ arg.name()|var_name }}Utf8
            {%- else %}
            {{ arg.name()|var_name }}
            {%- endif %}
            {%- if !loop.last %}, {% endif %}
            {%- endfor %}
        );
        {%- else %}
        {%- call cs::ffi_call_binding(func, "") %}
        return {{ return_type|lift_fn }}(_uniffiResult);
        {%- endif %}
    }
{% when None %}
    public static void {{ func.name()|fn_name }}({% call cs::arg_list_decl(func) %}) {
        {%- if config.high_performance_strings() && func|has_string_arguments %}
        {#/* Delegate to the span variant, encoding string arguments as UTF-8 */#}
        {%- for arg in func.arguments() %}
        {%- if arg|type_name(ci) == "string" %}
        var {{ arg.name()|var_name }}Utf8 = System.Text.Encoding.UTF8.GetBytes({{ arg.name()|var_name }});
        {%- endif %}
        {%- endfor %}
        {{ func.name()|fn_name }}Span(
            {%- for arg in func.arguments() %}
            {%- if arg|type_name(ci) == "string" %}
            {{ arg.name()|var_name }}Utf8
            {%- else %}
            {{ arg.name()|var_name }}
            {%- endif %}
            {%- if !loop.last %}, {% endif %}
            {%- endfor %}
        );
        {%- else %}
        {%- call cs::ffi_call_binding(func, "") %}
        {%- endif %}
    }
{% endmatch %}
{% endif  %}

{#/* Generate high-performance span version for functions with string args */#}
{%- if config.high_performance_strings() %}
{%- if !func.is_async() %}
{%- if func|has_string_arguments %}

{#/* Generate the span variant of the function */#}
    /// <summary>
    /// High-performance variant using ReadOnlySpan&lt;byte&gt; for zero-copy handling.
    /// String parameters are passed as UTF-8 encoded spans, avoiding RustBuffer
    /// allocations.
    /// </summary>
{%- call cs::method_throws_annotation(func.throws_type()) %}
{%- match func.return_type() -%}
{%- when Some with (return_type) %}
    public static unsafe {{ return_type|type_name(ci) }} {{ func.name()|fn_name }}Span(
        {%- for arg in func.arguments() -%}
            {%- if arg|type_name(ci) == "string" -%}
                ReadOnlySpan<byte> {{ arg.name()|var_name }}Utf8
            {%- else -%}
                {{ arg|type_name(ci) }} {{ arg.name()|var_name }}
            {%- endif -%}
            {%- if !loop.last %}, {% endif -%}
        {%- endfor -%}
    ) {
        UniffiRustCallStatus _status = default;

        {%- for arg in func.arguments() %}
        {%- if arg|type_name(ci) == "string" %}
        fixed (byte* {{ arg.name()|var_name }}Ptr = {{ arg.name()|var_name }}Utf8)
        {%- endif %}
        {%- endfor %}
        {
            var result = _UniFFILib.{{ func.ffi_func().name() }}_raw(
                {%- for arg in func.arguments() %}
                {%- if arg|type_name(ci) == "string" %}
                {{ arg.name()|var_name }}Ptr,
                {{ arg.name()|var_name }}Utf8.Length
                {%- else %}
                {{ arg|lower_fn }}({{ arg.name()|var_name }})
                {%- endif %}
                {%- if !loop.last %},{% else %},{% endif %}
                {%- endfor %}
                ref _status
            );

            {%- match func.throws_type() %}
            {%- when Some with (e) %}
            _UniffiHelpers.CheckCallStatus({{ e|error_converter_name }}.INSTANCE, ref _status);
            {%- else %}
            _UniffiHelpers.CheckCallStatus(NullCallStatusErrorHandler.INSTANCE, ref _status);
            {%- endmatch %}

            return {{ return_type|lift_fn }}(result);
        }
    }
{% when None %}
    public static unsafe void {{ func.name()|fn_name }}Span(
        {%- for arg in func.arguments() -%}
            {%- if arg|type_name(ci) == "string" -%}
                ReadOnlySpan<byte> {{ arg.name()|var_name }}Utf8
            {%- else -%}
                {{ arg|type_name(ci) }} {{ arg.name()|var_name }}
            {%- endif -%}
            {%- if !loop.last %}, {% endif -%}
        {%- endfor -%}
    ) {
        UniffiRustCallStatus _status = default;

        {%- for arg in func.arguments() %}
        {%- if arg|type_name(ci) == "string" %}
        fixed (byte* {{ arg.name()|var_name }}Ptr = {{ arg.name()|var_name }}Utf8)
        {%- endif %}
        {%- endfor %}
        {
            _UniFFILib.{{ func.ffi_func().name() }}_raw(
                {%- for arg in func.arguments() %}
                {%- if arg|type_name(ci) == "string" %}
                {{ arg.name()|var_name }}Ptr,
                {{ arg.name()|var_name }}Utf8.Length
                {%- else %}
                {{ arg|lower_fn }}({{ arg.name()|var_name }})
                {%- endif %}
                {%- if !loop.last %},{% else %},{% endif %}
                {%- endfor %}
                ref _status
            );

            {%- match func.throws_type() %}
            {%- when Some with (e) %}
            _UniffiHelpers.CheckCallStatus({{ e|error_converter_name }}.INSTANCE, ref _status);
            {%- else %}
            _UniffiHelpers.CheckCallStatus(NullCallStatusErrorHandler.INSTANCE, ref _status);
            {%- endmatch %}
        }
    }
{% endmatch %}
{% endif %}
{%- endif %}
{%- endif %}
