#pragma once
#include "forward.h"

namespace lightz
{
	// Convert a string to a method
	enum class EMethod
	{
		INVALID,
		GET,
		POST,
		PUT,
		DELETE
	};
	inline char const* ToString(EMethod method)
	{
		switch (method)
		{
		case EMethod::INVALID: return "INVALID";
		case EMethod::GET: return "GET";
		case EMethod::POST: return "POST";
		case EMethod::PUT: return "PUT";
		case EMethod::DELETE: return "DELETE";
		default: throw std::runtime_error("Unknown method");
		}
	}
	inline EMethod ToMethod(std::string_view verb)
	{
		if (verb == "GET") return EMethod::GET;
		if (verb == "POST") return EMethod::POST;
		if (verb == "PUT") return EMethod::PUT;
		if (verb == "DELETE") return EMethod::DELETE;
		return EMethod::INVALID;
	}

	// HTTP response codes
	enum class EResponseCode
	{
		OK = 200,
		BAD_REQUEST = 400,
		NOT_FOUND = 404,
		REQUEST_TIMEOUT = 408,
		INTERNAL_SERVER_ERROR = 500,
	};
	inline char const* ToString(EResponseCode code)
	{
		switch (code)
		{
		case EResponseCode::OK: return "OK";
		case EResponseCode::BAD_REQUEST: return "Bad Request";
		case EResponseCode::NOT_FOUND: return "Not Found";
		case EResponseCode::REQUEST_TIMEOUT: return "Request Timeout";
		case EResponseCode::INTERNAL_SERVER_ERROR: return "Internal Server Error";
		default: throw std::runtime_error("Unknown response code");
		}
	}

	// Content types
	enum class EContentType
	{
		TEXT_PLAIN,
		TEXT_HTML,
		TEXT_CSS,
		TEXT_JAVASCRIPT,
		TEXT_JSON,
		IMAGE_PNG,
		IMAGE_JPEG,
		IMAGE_GIF,
		IMAGE_SVG,
		IMAGE_X_ICON,
		APPLICATION_JSON,
		APPLICATION_XML,
		APPLICATION_OCTET_STREAM,
	};
	inline char const* ToString(EContentType ctype)
	{
		switch (ctype)
		{
		case EContentType::TEXT_PLAIN: return "text/plain";
		case EContentType::TEXT_HTML: return "text/html";
		case EContentType::TEXT_CSS: return "text/css";
		case EContentType::TEXT_JAVASCRIPT: return "text/javascript";
		case EContentType::TEXT_JSON: return "text/json";
		case EContentType::IMAGE_PNG: return "image/png";
		case EContentType::IMAGE_JPEG: return "image/jpeg";
		case EContentType::IMAGE_GIF: return "image/gif";
		case EContentType::IMAGE_SVG: return "image/svg+xml";
		case EContentType::IMAGE_X_ICON: return "image/x-icon";
		case EContentType::APPLICATION_JSON: return "application/json";
		case EContentType::APPLICATION_XML: return "application/xml";
		case EContentType::APPLICATION_OCTET_STREAM: return "application/octet-stream";
		default: throw std::runtime_error("Unknown content type");
		}
	}
}